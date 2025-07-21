namespace MySensorApi.Models
{
    public class SensorData
    {
        public int? Id { get; set; } // ID об'экта
        public string? RoomName { get; set; } //Назва кімнати
        public double? TemperatureDht { get; set; } //Температура, Градуси Цельсія (°C)
        public double? HumidityDht { get; set; } //Вологість, Відсотки (%)
        public string? GasDetected { get; set; } = string.Empty; //Детектор газа, Текст: "Yes" / "No"
        public string? Light { get; set; } = string.Empty; //Детектор світлу, Текст: "Bright" / "Dark"
        public double? Pressure { get; set; } //Тиск, Гектопаскалі (hPa)
        public double? Altitude { get; set; } //Висота над рівнем моря, Метри (m)
        public double? TemperatureBme { get; set; } //Температура, Градуси Цельсія (°C)
        public double? HumidityBme { get; set; } //Вологість, Відсотки (%)
        public double? MQ2Analog { get; set; } //Детектор газа, ppm
        public double? LightAnalog { get; set; } //Детектор світлу, lux
        public double? MQ2AnalogPercent { get; set; } //Детектор газа, Відсотки (%)
        public double? LightAnalogPercent { get; set; } //Детектор світлу, Відсотки (%)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
