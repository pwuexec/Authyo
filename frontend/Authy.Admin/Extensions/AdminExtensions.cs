using Microsoft.AspNetCore.Builder;
using Authy.Admin.Components;
using Authy.Admin.Services;
using Authy.Application.Extensions;
using Authy.Application.Data;

namespace Authy.Admin.Extensions;

public static class AdminExtensions
{
    public static IServiceCollection AddAdminServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddRazorComponents()
            .AddInteractiveServerComponents()
            .Services
            .AddHttpContextAccessor()
            .AddScoped<IAdminContext, AdminContext>()
            .AddApplication()
            .AddPersistence(configuration)
            .AddHandlers(typeof(AdminExtensions).Assembly);

        return services;
    }

    public static WebApplication UseAdminMiddleware(this WebApplication app)
    {
        app.ApplyMigrations();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true)
                .UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true)
            .UseHttpsRedirection()
            .UseAntiforgery();

        return app;
    }

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapStaticAssets();
        
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
