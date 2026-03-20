using Microsoft.EntityFrameworkCore;
using ShiftSwap.Models;

namespace ShiftSwap.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Location>()
                .HasOne(l => l.Company)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany()
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Location)
                .WithMany(l => l.Shifts)
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.User)
                .WithMany(u => u.Shifts)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.Shift)
                .WithMany(s => s.SwapRequests)
                .HasForeignKey(r => r.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.FromUser)
                .WithMany()
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.ToUser)
                .WithMany()
                .HasForeignKey(r => r.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // soft delete filter
            modelBuilder.Entity<Shift>()
                .HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
