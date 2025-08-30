namespace MySensorApi.DTO.Recommendations
{
    public class RecommendationsDto
    {
        public string ChipId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public List<string> Advice { get; set; } = new();
    }
}
