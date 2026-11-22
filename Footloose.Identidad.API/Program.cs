using Footloose.Identidad.API.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("SupabaseConnection")
    ?? throw new InvalidOperationException("Falta configurar 'SupabaseConnection' en appsettings.json.");

builder.Services.AddDbContext<FootlooseDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.MapOpenApi();
app.MapScalarApiReference();

app.UseAuthorization();
app.MapControllers();

app.Run();