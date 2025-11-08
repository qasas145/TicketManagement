using Microsoft.EntityFrameworkCore;
using Serilog;
using TicketManagement.Infrastructure;
using TicketManagement.Infrastructure.Observability;
using TicketManagement.Services.Inventory.BackgroundServices;
using TicketManagement.Services.Inventory.Controllers;
using TicketManagement.Services.Inventory.Data;
using TicketManagement.Services.Inventory.Repositories;
using TicketManagement.Services.Inventory.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Inventory")
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDb")));

// Repositories
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

// Services
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Background Services
builder.Services.AddHostedService<ReservationCleanupService>();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

