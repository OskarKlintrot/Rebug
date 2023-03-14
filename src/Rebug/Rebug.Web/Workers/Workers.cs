namespace Rebug.Web.Workers;

public sealed class Mateo : Worker
{
    public Mateo(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Mateo));

        return Task.CompletedTask;
    }
}

public sealed class Bautista : Worker
{
    public Bautista(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Bautista));

        return Task.CompletedTask;
    }
}

public sealed class Juan : Worker
{
    public Juan(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Juan));

        return Task.CompletedTask;
    }
}

public sealed class Felipe : Worker
{
    public Felipe(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Felipe));

        return Task.CompletedTask;
    }
}

public sealed class Bruno : Worker
{
    public Bruno(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Bruno));

        return Task.CompletedTask;
    }
}

public sealed class Noah : Worker
{
    public Noah(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Noah));

        return Task.CompletedTask;
    }
}

public sealed class Benicio : Worker
{
    public Benicio(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Benicio));

        return Task.CompletedTask;
    }
}

public sealed class Thiago : Worker
{
    public Thiago(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Thiago));

        return Task.CompletedTask;
    }
}

public sealed class Ciro : Worker
{
    public Ciro(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Ciro));

        return Task.CompletedTask;
    }
}

public sealed class Liam : Worker
{
    public Liam(WorkerDependencies dependencies)
        : base(dependencies) { }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job {Name} executed.", nameof(Liam));

        return Task.CompletedTask;
    }
}
