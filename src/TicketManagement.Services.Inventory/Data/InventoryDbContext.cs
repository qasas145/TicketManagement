using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Inventory.Entities;

namespace TicketManagement.Services.Inventory.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Seat> Seats { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seat configuration
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");
            entity.HasKey(e => e.SeatId);
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.SeatNumber).HasColumnName("seat_number").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Section).HasColumnName("section").HasMaxLength(50);
            entity.Property(e => e.RowNumber).HasColumnName("row_number").HasMaxLength(10);
            entity.Property(e => e.SeatType).HasColumnName("seat_type").HasConversion<string>();
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.ReservedBy).HasColumnName("reserved_by").HasMaxLength(50);
            entity.Property(e => e.ReservedUntil).HasColumnName("reserved_until");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.EventId, e.SeatNumber }).IsUnique().HasDatabaseName("uk_event_seat");
            entity.HasIndex(e => new { e.EventId, e.Status }).HasDatabaseName("idx_event_status");
            entity.HasIndex(e => e.ReservedUntil).HasDatabaseName("idx_reserved_until");
        });

        // Reservation configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(e => e.ReservationId);
            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");
            entity.Property(e => e.SeatId).HasColumnName("seat_id").IsRequired();
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").HasMaxLength(100);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SeatId).HasDatabaseName("idx_seat_id");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_expires_at");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
        });
    }
}

