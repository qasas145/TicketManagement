using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Events.Data;
using TicketManagement.Services.Search.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database connection to Events DB for search
builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventsDb")));

// Services
builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();

