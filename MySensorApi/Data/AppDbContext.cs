using Microsoft.EntityFrameworkCore;
using MySensorApi.Models;

namespace MySensorApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // DbSet
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

            // USER -> ROLE
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            // SENSOR OWNERSHIP -> USER
            modelBuilder.Entity<SensorOwnership>()
                .HasOne(so => so.User)
                .WithMany(u => u.SensorOwnerships)
                .HasForeignKey(so => so.UserId);

            // SETTINGS USER ADJUSTMENT -> USER
            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.User)
                .WithMany()
                .HasForeignKey(adjust => adjust.UserId);

            // SETTINGS USER ADJUSTMENT -> SETTING
            modelBuilder.Entity<SettingsUserAdjustment>()
                .HasOne(adjust => adjust.Setting)
                .WithMany(setting => setting.Adjustments)
                .HasForeignKey(adjust => adjust.SettingId);

            // SENSOR DATA REQUIRED FIELDS
            modelBuilder.Entity<SensorData>()
                .Property(sd => sd.ChipId)
                .IsRequired();

            // SENSOR OWNERSHIP UNIQUE CONSTRAINT
            modelBuilder.Entity<SensorOwnership>()
                .HasIndex(so => new { so.ChipId, so.UserId })
                .IsUnique();

            // STRING LENGTH CONSTRAINTS
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SensorOwnership>()
                .Property(so => so.RoomName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Setting>()
                .Property(s => s.ParameterName)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
