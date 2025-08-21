namespace MySensorApi.Models
{
    public class ComfortRecommendation
    {
        public int Id { get; set; }

        // FK -> SensorOwnership
        public int SensorOwnershipId { get; set; }
        public SensorOwnership SensorOwnership { get; set; } = null!;

        public string Recommendation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}