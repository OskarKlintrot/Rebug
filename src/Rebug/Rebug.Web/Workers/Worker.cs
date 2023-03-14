using Medallion.Threading;
using Microsoft.ApplicationInsights;

namespace Rebug.Web.Workers;

public sealed record WorkerDependencies(
    IServiceScopeFactory ServiceScopeFactory,
    TelemetryClient TelemetryClient,
    IDistributedLockProvider DistributedLockProvider,
    ILoggerFactory LoggerFactory,
    IHostEnvironment HostEnvironment
);

public abstract class Worker : IHostedService, IDisposable
{
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;
    private bool _disposedValue;
    private readonly WorkerDependencies _dependencies;
    private readonly string _jobName;
    private readonly string _logCategoryName;

    protected readonly IServiceScopeFactory _serviceScopeFactory;
    protected readonly ILogger _logger;

    protected Worker(WorkerDependencies dependencies)
    {
        _dependencies = dependencies;
        _serviceScopeFactory = _dependencies.ServiceScopeFactory;

        _jobName = GetType().Name;
        _logCategoryName = GetType().FullName ?? GetType().Name;
        _logger = _dependencies.LoggerFactory.CreateLogger(_logCategoryName);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create linked token to allow cancelling executing task from provided token
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _stoppingCts.Token.Register(
            () =>
                _logger.LogInformation("Cancellation has been requested for {Job Name}.", _jobName)
        );

        _executingTask = Task.Run(
            () =>
                ExecuteContinuously(
                    token =>
                    {
                        var @lock = new NonConcurrentLock(
                            _jobName,
                            _jobName,
                            _dependencies.DistributedLockProvider,
                            _dependencies.TelemetryClient,
                            _dependencies.LoggerFactory.CreateLogger<NonConcurrentLock>()
                        );

                        return @lock.ExecuteAsync(
                            async cts =>
                            {
                                while (!cts.IsCancellationRequested)
                                {
                                    await ExecuteAsync(cts);

                                    await Delay.TryWait(TimeSpan.FromMinutes(1), cts);
                                }
                            },
                            token
                        );
                    },
                    _stoppingCts.Token
                ),
            _stoppingCts.Token
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="BackgroundService.ExecuteAsync(CancellationToken)"/>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    private async Task ExecuteContinuously(
        Func<CancellationToken, Task> executionTaskAsync,
        CancellationToken stoppingToken
    )
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await executionTaskAsync(stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    // Take a break and hope that whatever cause the lock to be lost will self heal
                    if (!await Delay.TryWait(TimeSpan.FromMinutes(1), stoppingToken))
                    {
                        _logger.LogInformation(
                            "Cancellation has been requested before {Wait} was reached, will not restart execution pipeline for {Job Name}.",
                            TimeSpan.FromMinutes(1),
                            _jobName
                        );

                        break;
                    }

                    _logger.LogInformation(
                        "Will restart execution pipeline for {Job Name}.",
                        _jobName
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Cancellation has been requested, will not restart execution pipeline for {Job Name}.",
                        _jobName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in {Namespace}.", ex.Source);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask is null)
        {
            // Job never started or have already been stopped.

            return;
        }

        _logger.LogInformation("Job {Job Name} is stopping.", _jobName);

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));

            _executingTask = null;

            _logger.LogInformation("Job {Job Name} has stopped.", _jobName);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stoppingCts?.Cancel();

                // Dispose managed state (managed objects)
                _stoppingCts?.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Wait indefinitely for a lock.
/// </summary>
internal sealed class NonConcurrentLock
{
    private readonly string _jobName;
    private readonly string _lockName;
    private readonly TimeSpan _waitBeforeTryingToObtainLockAfterFailedDbConnection =
        TimeSpan.FromSeconds(5);
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly ILogger<NonConcurrentLock> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly Dictionary<string, object> _state = new();

    public NonConcurrentLock(
        string jobName,
        string lockName,
        IDistributedLockProvider distributedLockProvider,
        TelemetryClient telemetryClient,
        ILogger<NonConcurrentLock> logger
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(jobName);
        ArgumentException.ThrowIfNullOrEmpty(lockName);
        ArgumentNullException.ThrowIfNull(distributedLockProvider);

        _jobName = jobName;
        _lockName = lockName;
        _distributedLockProvider = distributedLockProvider;
        _logger = logger;
        _telemetryClient = telemetryClient;

        _state["Job"] = "true";
        _state["Job Name"] = _jobName;
        _state["Lock Name"] = _lockName;
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> nextAsync,
        CancellationToken stoppingToken
    )
    {
        using var _ = _logger.BeginScope(_state);

        await using var handle = await GetHandleAsync(stoppingToken);

        if (handle is null)
        {
            return; // We never got the lock.
        }

        CancellationTokenRegistration? cancellationTokenRegistration = null;

        try
        {
            cancellationTokenRegistration = handle.HandleLostToken.Register(() =>
            {
                // HandleLostToken.Register is to slow to use for anything else than logging.
                _logger.LogError("Lost lock for job {Job Name}.", _jobName);

                _telemetryClient.TrackEvent(
                    new($"Lost lock for {_jobName}")
                    {
                        Properties = { { "Job Name", _jobName }, { "Lock State", "Lost" } },
                    }
                );
            });

            if (!handle.HandleLostToken.CanBeCanceled)
            {
                _logger.LogWarning("Implementation does not support lost handle detection");
            }

            using var stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken,
                handle.HandleLostToken
            );

            if (stoppingCts.Token.IsCancellationRequested)
            {
                return;
            }

            _logger.LogInformation("Acquired lock for {Job Name}.", _jobName);

            _telemetryClient.TrackEvent(
                new($"Acquired lock for {_jobName}")
                {
                    Properties = { { "Job Name", _jobName }, { "Lock State", "Acquired" } },
                }
            );

            await nextAsync(stoppingCts.Token);
        }
        finally
        {
            if (cancellationTokenRegistration.HasValue)
            {
                await cancellationTokenRegistration.Value.DisposeAsync();
            }

            if (!handle.WasLost())
            {
                _logger.LogInformation("Released lock for {Job Name}.", _jobName);

                _telemetryClient.TrackEvent(
                    new($"Released lock for {_jobName}")
                    {
                        Properties = { { "Job Name", _jobName }, { "Lock State", "Released" } },
                    }
                );
            }
        }
    }

    private async Task<IDistributedSynchronizationHandle?> GetHandleAsync(
        CancellationToken cancellationToken
    )
    {
        using var _ = _logger.BeginScope(_state);

        while (!cancellationToken.IsCancellationRequested)
        {
            var failure = true;

            try
            {
                _logger.LogInformation("Trying to acquire lock for {Job Name}", _jobName);

                var handle = await _distributedLockProvider.AcquireLockAsync(
                    _lockName,
                    timeout: null,
                    cancellationToken: cancellationToken
                );

                failure = false;

                return handle;
            }
            catch (Microsoft.Data.SqlClient.SqlException exception)
            {
                _logger.LogError(
                    exception,
                    "SQL exception {Number} where thrown while trying to acquiring lock for {Job Name}.",
                    exception.Number,
                    _jobName
                );

                _telemetryClient.TrackEvent(
                    new($"Failed acquiring lock for {_jobName} (SQL exception)")
                    {
                        Properties =
                        {
                            { "Job Name", _jobName },
                            { "Lock State", "SQL Exception" }
                        },
                    }
                );
            }
            catch (InvalidOperationException exception)
                when (exception.Message.Contains(
                        "The connection's current state is closed.",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            {
                _logger.LogError(
                    exception,
                    "Connection where closed when trying to acquire lock for {Job Name}.",
                    _jobName
                );

                _telemetryClient.TrackEvent(
                    new($"Failed acquiring lock for {_jobName} (connection closed)")
                    {
                        Properties =
                        {
                            { "Job Name", _jobName },
                            { "Lock State", "No Connection" }
                        },
                    }
                );
            }
            catch (OperationCanceledException exception)
                when (cancellationToken.IsCancellationRequested
                    && exception.InnerException?.Message.Contains(
                        "Operation cancelled by user.",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                )
            {
                failure = false;

                _logger.LogInformation("Cancellation is requested, stop waiting for lock.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Something went wrong when trying to acquire lock for {Job Name}.",
                    _jobName
                );

                _telemetryClient.TrackEvent(
                    new($"Failed acquiring lock for {_jobName} (exception)")
                    {
                        Properties =
                        {
                            { "Job Name", _jobName },
                            { "Lock State", "Unknown" },
                            { "Exception", exception.GetType().Name }
                        },
                    }
                );
            }
            finally
            {
                if (failure)
                {
                    await Delay.TryWait(
                        _waitBeforeTryingToObtainLockAfterFailedDbConnection,
                        cancellationToken
                    );
                }
            }
        }

        return null;
    }
}

file static class DistributedSynchronizationHandleExtensions
{
    /// <inheritdoc cref="IDistributedSynchronizationHandle.HandleLostToken" />
    public static bool WasLost(this IDistributedSynchronizationHandle handle) =>
        handle.HandleLostToken.CanBeCanceled && handle.HandleLostToken.IsCancellationRequested;
}

file static class Delay
{
    /// <summary>
    /// Wraps <see cref="Task.Delay(TimeSpan, CancellationToken)"/> but won't throw <see cref="TaskCanceledException"/>.
    /// </summary>
    /// <param name="delay"><inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)" path='//param[@name="delay"]'/></param>
    /// <param name="cancellationToken"><inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)" path='//param[@name="cancellationToken"]'/></param>
    /// <returns><see langword="True"/> unless <paramref name="cancellationToken"/> was cancelled.</returns>
    public static async Task<bool> TryWait(
        TimeSpan delay,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return false;
        }

        return true;
    }
}
