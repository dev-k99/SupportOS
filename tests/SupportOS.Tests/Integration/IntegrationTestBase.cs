using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportOS.Application;
using SupportOS.Application.Interfaces;
using SupportOS.Domain.Interfaces;
using SupportOS.Infrastructure.Persistence;
using SupportOS.Infrastructure.Repositories;
using SupportOS.Infrastructure.Services;

namespace SupportOS.Tests.Integration;

/// <summary>
/// Base class for integration tests. Spins up the full MediatR pipeline
/// (all behaviors: Idempotency → Logging → Validation → Performance)
/// against an isolated EF Core InMemory database.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IMediator Mediator { get; private set; } = null!;
    protected SupportOSDbContext DbContext { get; private set; } = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var dbName = Guid.NewGuid().ToString(); // isolated DB per test

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // InMemory DB — no AuditInterceptor in tests (it needs real HttpContext)
        services.AddDbContext<SupportOSDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        // Full MediatR pipeline + FluentValidation
        services.AddApplication();

        // Infrastructure — repositories and idempotency
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        _provider = services.BuildServiceProvider();

        DbContext = _provider.GetRequiredService<SupportOSDbContext>();
        Mediator  = _provider.GetRequiredService<IMediator>();

        await DbContext.Database.EnsureCreatedAsync();
        await SeedBaseDataAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _provider.DisposeAsync();
    }

    // Seeds the minimum data every test needs (categories + users)
    protected virtual Task SeedBaseDataAsync() => Task.CompletedTask;
}
