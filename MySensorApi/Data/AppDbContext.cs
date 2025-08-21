using Microsoft.EntityFrameworkCore;
using MySensorApi.Models;

namespace MySensorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSet-и
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
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .Property(u => u.Username).HasMaxLength(50).IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email).HasMaxLength(100).IsRequired();

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName).HasMaxLength(50).IsRequired();

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "User" },
                new Role { Id = 2, RoleName = "Admin" }
            );

            // ------------------ OWNERSHIP ------------------
            modelBuilder.Entity<SensorOwnership>()
                .HasOne(so => so.User)
                .WithMany(u => u.SensorOwnerships)
                .HasForeignKey(so => so.UserId);

            modelBuilder.Entity<SensorOwnership>()
                .Property(so => so.ChipId).HasMaxLength(32).IsRequired();

            modelBuilder.Entity<SensorOwnership>()
                .HasIndex(so => so.ChipId);

            modelBuilder.Entity<SensorOwnership>()
                .Property(so => so.RoomName).HasMaxLength(100).IsRequired();

            // Унікальність зв’язки користувач+чип
            modelBuilder.Entity<SensorOwnership>()
                .HasIndex(so => new { so.ChipId, so.UserId })
                .IsUnique();

            // ------------------ SENSOR DATA ------------------
            modelBuilder.Entity<SensorData>()
                .Property(sd => sd.ChipId).IsRequired();

            // дефолт часу (рекомендація)
            modelBuilder.Entity<SensorData>()
                .Property(sd => sd.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ------------------ SETTINGS ------------------
            modelBuilder.Entity<Setting>()
                .Property(s => s.ParameterName).HasMaxLength(100).IsRequired();

            modelBuilder.Entity<Setting>()
                .HasIndex(s => s.ParameterName)
                .IsUnique(); // temperature / humidity / gas по одному

            modelBuilder.Entity<Setting>()
                .Property(s => s.LowValueMessage).HasMaxLength(500);

            modelBuilder.Entity<Setting>()
                .Property(s => s.HighValueMessage).HasMaxLength(500);

            // ------------------ SETTINGS USER ADJUSTMENT ------------------
            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.User)
                .WithMany()
                .HasForeignKey(adjust => adjust.UserId);

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.Setting)
                .WithMany(setting => setting.Adjustments)
                .HasForeignKey(adjust => adjust.SettingId);

            modelBuilder.Entity<SettingsUserAdjustment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // корисний індекс: остання дельта на користувача+настройку
            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasIndex(a => new { a.UserId, a.SettingId, a.CreatedAt });

            // ------------------ COMFORT RECOMMENDATION ------------------
            modelBuilder.Entity<ComfortRecommendation>()
                .HasOne(c => c.SensorOwnership)
                .WithMany() // або .WithMany(o => o.Recommendations) якщо є колекція
                .HasForeignKey(c => c.SensorOwnershipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComfortRecommendation>()
                .Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // швидкі запити "остання рекомендація по кімнаті"
            modelBuilder.Entity<ComfortRecommendation>()
                .HasIndex(c => new { c.SensorOwnershipId, c.CreatedAt });

            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasIndex(a => new { a.UserId, a.SettingId, a.Version })
                .IsUnique(); // уникнення гонок версій

            modelBuilder.Entity<SettingsUserAdjustment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Seed Settings
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
