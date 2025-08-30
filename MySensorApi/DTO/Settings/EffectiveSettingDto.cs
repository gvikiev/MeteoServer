namespace MySensorApi.DTO.Settings
{
    public class EffectiveSettingDto
    {
        public string ParameterName { get; set; } = string.Empty; // "temperature" | "humidity" | "gas"
        public float? LowValue { get; set; }
        public float? HighValue { get; set; }
        public string? LowValueMessage { get; set; }
        public string? HighValueMessage { get; set; }
    }
}
