using Accounting.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// --- Connection string (bulletproof) ---
var cs =
    builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["CONNECTION_STRING"];

if (string.IsNullOrWhiteSpace(cs))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Add ConnectionStrings:Default (or DefaultConnection) to appsettings.json " +
        "or set environment variable CONNECTION_STRING."
    );
}

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

// --- CORS for Nuxt dev server ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("nuxt", p =>
        p.WithOrigins("http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors("nuxt");

app.MapControllers();

app.Run();