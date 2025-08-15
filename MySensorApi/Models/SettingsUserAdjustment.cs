namespace MySensorApi.Models
{
    public class SettingsUserAdjustment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SettingId { get; set; }
        public float LowValueAdjustment { get; set; }
        public float HighValueAdjustment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Setting Setting { get; set; } = null!;
    }
}
