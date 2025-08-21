namespace MySensorApi.DTO
{
    public class SettingUpsertDto
    {
        public string ParameterName { get; set; } = "";
        public float? LowValue { get; set; }
        public float? HighValue { get; set; }
        public string? LowValueMessage { get; set; }
        public string? HighValueMessage { get; set; }
    }
}
