namespace MySensorApi.DTO.SensorData
{
    public class SensorDataDto
    {
        public string? ChipId { get; set; }
        public string? RoomName { get; set; }

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
    }
}
