using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
     .AllowAnyHeader().AllowAnyMethod()));

var connectionString =
    Environment.GetEnvironmentVariable("STOCKFLOW_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=stockflow;Username=stockflow;Password=CHANGEME";

builder.Services.AddDbContext<StockFlowDbContext>(opt =>
    opt.UseNpgsql(connectionString, npg => npg.MigrationsAssembly("StockFlow.Api")));

var app = builder.Build();

app.UseCors();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
}

app.Run();
