using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Events.Entities;

namespace TicketManagement.Services.Events.Data;

public class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EventName).HasColumnName("event_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.EventDate).HasColumnName("event_date").IsRequired();
            entity.Property(e => e.VenueName).HasColumnName("venue_name").HasMaxLength(255);
            entity.Property(e => e.TotalSeats).HasColumnName("total_seats").IsRequired();
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.SaleStartTime).HasColumnName("sale_start_time");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SaleStartTime).HasDatabaseName("idx_sale_start_time");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
        });
    }
}

