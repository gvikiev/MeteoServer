namespace MySensorApi.DTO
{
    public class SensorDataDto
    {
        public string? RoomName { get; set; }
        public double? TemperatureDht { get; set; }
        public double? HumidityDht { get; set; }
        public string? GasDetected { get; set; } = string.Empty;
        public double? Pressure { get; set; }
        public double? Altitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
