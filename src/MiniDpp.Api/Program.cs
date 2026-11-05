using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Endpoints;
using MiniDpp.Api.Importing;
using MiniDpp.Api.Passports;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(server =>
    server.Limits.MaxRequestBodySize = 2 * ImportEndpoints.MaxUploadBytes);
builder.Services.Configure<FormOptions>(form =>
    form.MultipartBodyLengthLimit = 2 * ImportEndpoints.MaxUploadBytes);
builder.Services.AddProblemDetails();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<PassportService>();
builder.Services.AddSingleton<PassportUrlBuilder>();
builder.Services.AddOptions<DppOptions>()
    .Bind(builder.Configuration.GetSection(DppOptions.SectionName))
    .Validate(options => DppOptions.IsValidPublicBaseUrl(options.PublicBaseUrl),
        "Dpp:PublicBaseUrl, yol veya sorgu içermeyen kök bir HTTP/HTTPS adresi olmalıdır.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection ayarı bulunamadı.");

var sqliteSettings = new SqliteConnectionStringBuilder(connectionString)
{
    ForeignKeys = true
};
sqliteSettings.DataSource = Path.Combine(
    builder.Environment.ContentRootPath, sqliteSettings.DataSource);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteSettings.ToString()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStaticFiles();

// ÖNEMLİ: Yeni kurulumda tabloları oluşturur; mevcut kayıtlara dokunmadan
// yalnızca uygulanmamış migration'ları çalıştırır. Tek süreçli demo içindir.
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapImportEndpoints();
app.MapDppEndpoints();

await app.RunAsync();

public partial class Program { }
