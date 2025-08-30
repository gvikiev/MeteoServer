using System;

namespace MySensorApi.DTO
{
    public class SensorDataInDto
    {
        public string ChipId { get; set; } = string.Empty;

        public float? TemperatureDht { get; set; }
        public float? HumidityDht { get; set; }

        public float? TemperatureBme { get; set; }
        public float? HumidityBme { get; set; }
        public float? Pressure { get; set; }
        public float? Altitude { get; set; }

        public bool? GasDetected { get; set; }
        public bool? Light { get; set; }

        public int? MQ2Analog { get; set; }
        public float? MQ2AnalogPercent { get; set; }

        public int? LightAnalog { get; set; }
        public float? LightAnalogPercent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // сервер виставляє час отримання
    }
}
