namespace MySensorApi.DTO
{
    public sealed class EffectiveSettingDto
    {
        public string ParameterName { get; set; } = null!;
        public float? LowValue { get; set; }
        public float? HighValue { get; set; }
        public string? LowValueMessage { get; set; }
        public string? HighValueMessage { get; set; }
    }
}
