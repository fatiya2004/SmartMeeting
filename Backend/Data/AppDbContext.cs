using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<Unavailability> Unavailabilities => Set<Unavailability>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MeetingMinutes> MeetingMinutes => Set<MeetingMinutes>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Ignore(u => u.FullName);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasOne(m => m.Room)
                  .WithMany(r => r.Meetings)
                  .HasForeignKey(m => m.RoomId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.CreatedBy)
                  .WithMany(u => u.CreatedMeetings)
                  .HasForeignKey(m => m.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.Date, m.RoomId });
        });

        modelBuilder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasKey(mp => new { mp.MeetingId, mp.UserId });

            entity.HasOne(mp => mp.Meeting)
                  .WithMany(m => m.Participants)
                  .HasForeignKey(mp => mp.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mp => mp.User)
                  .WithMany(u => u.Participations)
                  .HasForeignKey(mp => mp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unavailability>(entity =>
        {
            entity.HasOne(u => u.User)
                  .WithMany(x => x.Unavailabilities)
                  .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(u => new { u.UserId, u.Date });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Meeting)
                  .WithMany()
                  .HasForeignKey(n => n.MeetingId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MeetingMinutes>(entity =>
        {
            entity.HasOne(mm => mm.Meeting)
                  .WithOne(m => m.Minutes)
                  .HasForeignKey<MeetingMinutes>(mm => mm.MeetingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
