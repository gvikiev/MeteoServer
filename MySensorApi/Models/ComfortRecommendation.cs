namespace MySensorApi.Models
{
    public class ComfortRecommendation
    {
        public int Id { get; set; }

        public int ChipId { get; set; }

        public SensorOwnership SensorOwnership { get; set; }

        public string Recommendation { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
