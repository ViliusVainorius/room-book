using Microsoft.EntityFrameworkCore;
using RoomBook.Domain.Models;

namespace RoomBook.Infrastructure.Context;

public class RoomBookDbContext : DbContext
{
    public RoomBookDbContext(
        DbContextOptions<RoomBookDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(r => r.Floor)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(r => r.Status)
                .HasConversion<string>();

            entity.HasOne(r => r.Room)
                .WithMany(room => room.Reservations)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Organizer)
                .WithMany(user => user.Reservations)
                .HasForeignKey(r => r.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}