using System.Diagnostics;
using Medallion.Threading;
using Medallion.Threading.SqlServer;
using Rebug.Web.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddTransient<WorkerDependencies>()
    .AddSingleton<IDistributedLockProvider>(
        provider =>
            new SqlDistributedSynchronizationProvider(
                provider.GetRequiredService<IConfiguration>().GetConnectionString("DbConnection")
                    ?? throw new UnreachableException("Missing connection string.")
            )
    )
    .AddHostedService<Mateo>()
    .AddHostedService<Bautista>()
    .AddHostedService<Juan>()
    .AddHostedService<Felipe>()
    .AddHostedService<Bruno>()
    .AddHostedService<Noah>()
    .AddHostedService<Benicio>()
    .AddHostedService<Thiago>()
    .AddHostedService<Ciro>()
    .AddHostedService<Liam>();

builder.Logging.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => "Hello world!").WithName("Get").WithOpenApi();

app.Run();
