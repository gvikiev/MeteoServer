namespace MySensorApi.Models
{
    public class ComfortRecommendation
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? RoomName { get; set; }
        public string? Recommendation { get; set; }
    }
}
