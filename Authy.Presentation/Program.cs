using Authy.Application.Extensions;
using Authy.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

app.UsePresentation();

app.Run();

