using Crm.Api.Endpoints;
using Crm.Infrastructure;
using Crm.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCrmInfrastructure();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

// В контейнере рядом с API лежит собранный фронт. В dev его нет - там работает Vite,
// поэтому статику подключаем только когда wwwroot реально существует.
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var servesSpa = Directory.Exists(wwwroot);

if (servesSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

app.MapGet("/api", () => Results.Ok(new
{
    service = "CRM Vova API",
    endpoints = new[]
    {
        "GET  /api/leads",
        "POST /api/leads",
        "POST /api/leads/{id}/qualify",
        "GET  /api/opportunities",
        "POST /api/opportunities/{id}/stage",
        "GET  /api/stages",
        "GET  /api/tasks",
        "POST /api/tasks/{id}/complete",
        "GET  /api/processes",
        "GET  /api/processes/{id}/log"
    }
}));

app.MapSalesEndpoints();
app.MapProcessEndpoints();

if (servesSpa)
{
    // Ограничение по префиксу обязательно: без него неизвестный /api/... вернул бы
    // страницу SPA с кодом 200 вместо честного 404.
    app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");
}

app.Run();
