namespace MySensorApi.DTO
{
    public sealed class AdjustmentAbsoluteRequestDto
    {
        public List<AdjustmentAbsoluteItemDto> Items { get; set; } = new();
    }

    public sealed class AdjustmentAbsoluteItemDto
    {
        public string ParameterName { get; set; } = null!; // "temperature" | "humidity" | ...
        public float? Low { get; set; }                     // абсолютне значення з UI (може бути null)
        public float? High { get; set; }                    // абсолютне значення з UI (може бути null)
    }

    // (опційно) зручно повертати відповідь
    public sealed class AdjustmentAbsoluteResponseDto
    {
        public int UserId { get; set; }
        public List<AdjustmentAppliedDto> Items { get; set; } = new();
    }

    public sealed class AdjustmentAppliedDto
    {
        public string ParameterName { get; set; } = null!;
        public float? BaseLow { get; set; }
        public float? BaseHigh { get; set; }
        public float LowDelta { get; set; }     // що записали в SettingsUserAdjustments
        public float HighDelta { get; set; }
        public int Version { get; set; }        // нова версія запису
        public float? EffectiveLow { get; set; }  // Base + Delta
        public float? EffectiveHigh { get; set; }
    }
}
