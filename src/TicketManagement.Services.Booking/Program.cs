using Microsoft.EntityFrameworkCore;
using Serilog;
using TicketManagement.Infrastructure;
using TicketManagement.Services.Booking.Clients;
using TicketManagement.Services.Booking.Data;
using TicketManagement.Services.Booking.Repositories;
using TicketManagement.Services.Booking.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Booking")
    .WriteTo.Console()
    .WriteTo.File("logs/booking-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BookingDb")));

// Repositories
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingSeatRepository, BookingSeatRepository>();
builder.Services.AddScoped<IIdempotencyKeyRepository, IdempotencyKeyRepository>();

// Clients
builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Inventory"] ?? "http://localhost:5001");
});
builder.Services.AddScoped<IInventoryServiceClient, InventoryServiceClient>();

builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Payment"] ?? "http://localhost:5004");
});
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();

// Services
builder.Services.AddScoped<IBookingService, BookingService>();

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
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

