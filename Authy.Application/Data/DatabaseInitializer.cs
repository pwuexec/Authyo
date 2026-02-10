using Authy.Application.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Authy.Application.Data;

internal class DatabaseInitializer(IServiceScopeFactory scopeFactory, IOptions<PersistenceOptions> persistenceOptions)
    : IHostedLifecycleService
{
    private readonly PersistenceOptions _persistenceOptions = persistenceOptions.Value;

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthyDbContext>();

        if (_persistenceOptions.Recreate)
        {
            await context.Database.EnsureDeletedAsync(cancellationToken);
        }

        if (context.Database.GetMigrations().Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
