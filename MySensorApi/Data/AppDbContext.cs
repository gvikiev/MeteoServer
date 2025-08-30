using Microsoft.EntityFrameworkCore;
using MySensorApi.Models;

namespace MySensorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SensorOwnership> SensorOwnerships { get; set; }
        public DbSet<SensorData> SensorData { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<SettingsUserAdjustment> SettingsUserAdjustments { get; set; }
        public DbSet<ComfortRecommendation> ComfortRecommendations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------ USER / ROLE ------------------
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .Property(u => u.Username).HasMaxLength(50).IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Email).HasMaxLength(100).IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.RefreshToken).HasMaxLength(200);

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName).HasMaxLength(50).IsRequired();

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "User" },
                new Role { Id = 2, RoleName = "Admin" }
            );

            // ------------------ OWNERSHIP ------------------
            modelBuilder.Entity<SensorOwnership>()
                .HasOne(so => so.User)
                .WithMany(u => u.SensorOwnerships)
                .HasForeignKey(so => so.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SensorOwnership>()
                .Property(so => so.ChipId).HasMaxLength(32).IsRequired();

            modelBuilder.Entity<SensorOwnership>()
                .HasIndex(so => so.ChipId);

            modelBuilder.Entity<SensorOwnership>()
                .Property(so => so.RoomName).HasMaxLength(100).IsRequired();

            modelBuilder.Entity<SensorOwnership>()
                .HasIndex(so => new { so.ChipId, so.UserId })
                .IsUnique();

            // ------------------ SENSOR DATA ------------------
            modelBuilder.Entity<SensorData>()
                .Property(sd => sd.ChipId).IsRequired();

            modelBuilder.Entity<SensorData>()
                .Property(sd => sd.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SensorData>()
                .HasIndex(sd => new { sd.ChipId, sd.CreatedAt });

            // ------------------ SETTINGS ------------------
            modelBuilder.Entity<Setting>()
                .Property(s => s.ParameterName).HasMaxLength(100).IsRequired();

            modelBuilder.Entity<Setting>()
                .HasIndex(s => s.ParameterName)
                .IsUnique();

            modelBuilder.Entity<Setting>()
                .Property(s => s.LowValueMessage).HasMaxLength(500);

            modelBuilder.Entity<Setting>()
                .Property(s => s.HighValueMessage).HasMaxLength(500);

            // ------------------ SETTINGS USER ADJUSTMENT ------------------
            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.User)
                .WithMany()
                .HasForeignKey(adjust => adjust.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.Setting)
                .WithMany(setting => setting.Adjustments)
                .HasForeignKey(adjust => adjust.SettingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(a => a.SensorOwnership)
                .WithMany()
                .HasForeignKey(a => a.SensorOwnershipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SettingsUserAdjustment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasIndex(a => new { a.UserId, a.SensorOwnershipId, a.SettingId, a.CreatedAt });

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasIndex(a => new { a.UserId, a.SettingId, a.SensorOwnershipId, a.Version })
                .IsUnique();

            // ------------------ COMFORT RECOMMENDATION ------------------
            modelBuilder.Entity<ComfortRecommendation>()
                .HasOne(c => c.SensorOwnership)
                .WithMany()
                .HasForeignKey(c => c.SensorOwnershipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComfortRecommendation>()
                .HasOne(c => c.SensorData)
                .WithOne(d => d.ComfortRecommendation)
                .HasForeignKey<ComfortRecommendation>(c => c.SensorDataId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComfortRecommendation>()
                .HasIndex(c => c.SensorDataId)
                .IsUnique();

            modelBuilder.Entity<ComfortRecommendation>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ------------------ Seed Settings ------------------
            modelBuilder.Entity<Setting>().HasData(
                new Setting
                {
                    Id = 1,
                    ParameterName = "temperature",
                    LowValue = 18,
                    HighValue = 25,
                    LowValueMessage = "Прохолодно. Зачиніть вікно або ввімкніть обігрів.",
                    HighValueMessage = "Занадто душно. Провітріть або ввімкніть кондиціонер."
                },
                new Setting
                {
                    Id = 2,
                    ParameterName = "humidity",
                    LowValue = 30,
                    HighValue = 60,
                    LowValueMessage = "Сухе повітря. Зволожте кімнату.",
                    HighValueMessage = "Висока вологість. Провітріть приміщення."
                },
                new Setting
                {
                    Id = 3,
                    ParameterName = "gas",
                    LowValue = null,
                    HighValue = 1,
                    LowValueMessage = null,
                    HighValueMessage = "Виявлено газ/забруднення. Провітріть негайно."
                }
            );
        }
    }
}
