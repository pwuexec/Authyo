using Authy.Presentation.Data;
using Authy.Presentation.Endpoints;
using Authy.Presentation.Filters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AuthyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=authy.db"));

builder.Services.AddScoped<PlatformOwnerFilter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthyDbContext>();
    db.Database.EnsureCreated();
}

app.MapOrganizationEndpoints();
app.MapUserEndpoints();
app.MapRoleEndpoints();
app.MapScopeEndpoints();

app.Run();
