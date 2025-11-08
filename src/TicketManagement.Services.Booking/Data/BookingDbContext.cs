using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Booking.Entities;

namespace TicketManagement.Services.Booking.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Entities.Booking> Bookings { get; set; }
    public DbSet<BookingSeat> BookingSeats { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Booking configuration
        modelBuilder.Entity<Entities.Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(e => e.BookingId);
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.PaymentId).HasColumnName("payment_id").HasMaxLength(100);
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasConversion<string>();
            entity.Property(e => e.BookingReference).HasColumnName("booking_reference").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_user_id");
            entity.HasIndex(e => e.BookingReference).IsUnique().HasDatabaseName("idx_booking_reference");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_status");
        });

        // BookingSeat configuration
        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.ToTable("booking_seats");
            entity.HasKey(e => e.BookingSeatId);
            entity.Property(e => e.BookingSeatId).HasColumnName("booking_seat_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id").IsRequired();
            entity.Property(e => e.SeatId).HasColumnName("seat_id").IsRequired();
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)").IsRequired();

            entity.HasIndex(e => new { e.BookingId, e.SeatId }).IsUnique().HasDatabaseName("uk_booking_seat");
            entity.HasIndex(e => e.SeatId).HasDatabaseName("idx_seat_id");
        });

        // IdempotencyKey configuration
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => e.IdempotencyKeyId);
            entity.Property(e => e.IdempotencyKeyId).HasColumnName("idempotency_key_id");
            entity.Property(e => e.Key).HasColumnName("key").HasMaxLength(255).IsRequired();
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();

            entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("idx_key");
        });
    }
}

