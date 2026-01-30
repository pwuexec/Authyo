using Authy.Application.Extensions;
using Authy.Application.Data;
using Authy.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

app.UsePresentation();

app.Run();

