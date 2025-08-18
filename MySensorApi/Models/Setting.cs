namespace MySensorApi.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public string ParameterName { get; set; } = null!;
        public float LowValue { get; set; }
        public float HighValue { get; set; }
        public string? LowValueMessage { get; set; }
        public string? HighValueMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SettingsUserAdjustment> Adjustments { get; set; } = new List<SettingsUserAdjustment>();
    }

}
