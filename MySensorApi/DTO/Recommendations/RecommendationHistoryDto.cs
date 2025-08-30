namespace MySensorApi.DTO.Recommendations
{
    public class RecommendationHistoryDto
    {
        public DateTime CreatedAt { get; set; }
        public string? RoomName { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}
