namespace MySensorApi.Models
{
    public class SensorData
    {
        public int? Id { get; set; }
        public string? RoomName { get; set; }
        public double? TemperatureDht { get; set; }
        public double? HumidityDht { get; set; }
        public string? GasDetected { get; set; } = string.Empty;
        public string? Light { get; set; } = string.Empty;
        public double? Pressure { get; set; }
        public double? Altitude { get; set; }
        public double? TemperatureBme { get; set; }
        public double? HumidityBme { get; set; }
        public int? MQ2Analog { get; set; }
        public int? LightAnalog { get; set; }
        public double? MQ2AnalogPercent { get; set; }
        public double? LightAnalogPercent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
