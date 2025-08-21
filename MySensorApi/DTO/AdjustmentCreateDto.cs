namespace MySensorApi.DTO
{
    public class AdjustmentCreateDto
    {
        public string ParameterName { get; set; } = ""; // "temperature" | "humidity" | "gas"
        public float LowValueAdjustment { get; set; }   // може бути 0
        public float HighValueAdjustment { get; set; }  // може бути 0
        public int? UserId { get; set; }                // опційно: адміністратор може задати іншому користувачу
    }
}
