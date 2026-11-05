using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

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

// ÖNEMLİ: Yeni kurulumda tabloları oluşturur; mevcut kayıtlara dokunmadan
// yalnızca uygulanmamış migration'ları çalıştırır. Tek süreçli demo içindir.
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();

public partial class Program { }
