using Authy.Admin.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdminServices(builder.Configuration);

var app = builder.Build();

app.UseAdminMiddleware();
app.MapAdminEndpoints();

app.Run();

