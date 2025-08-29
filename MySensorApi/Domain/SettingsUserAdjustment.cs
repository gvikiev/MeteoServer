namespace MySensorApi.Models
{
    public class SettingsUserAdjustment
    {
        public int Id { get; set; }

        // Ключ ресурсу дельт
        public int UserId { get; set; }
        public int SettingId { get; set; }

        public int? SensorOwnershipId { get; set; }

        // Значення дельт
        public float LowValueAdjustment { get; set; }
        public float HighValueAdjustment { get; set; }

        // Версія (для ETag "userId-settingId-version")
        public int Version { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні
        public User User { get; set; } = null!;
        public Setting Setting { get; set; } = null!;
        public SensorOwnership? SensorOwnership { get; set; } = null!;
    }
}
