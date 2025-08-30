namespace MySensorApi.Models
{
    public class SensorData
    {
        public int Id { get; set; }
        public string ChipId { get; set; } = null!;
        public float? TemperatureDht { get; set; }
        public float? HumidityDht { get; set; }
        public bool? GasDetected { get; set; }
        public bool? Light { get; set; }
        public float? Pressure { get; set; }
        public float? Altitude { get; set; }
        public float? TemperatureBme { get; set; }
        public float? HumidityBme { get; set; }
        public int? Mq2Analog { get; set; }
        public float? Mq2AnalogPercent { get; set; }
        public int? LightAnalog { get; set; }
        public float? LightAnalogPercent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ComfortRecommendation? ComfortRecommendation { get; set; }
    }
}
