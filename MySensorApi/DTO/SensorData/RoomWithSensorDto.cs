namespace MySensorApi.DTO
{
    public class RoomWithSensorDto
    {
        public int Id { get; set; }
        public string ChipId { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public string ImageName { get; set; } = null!;
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
    }
}
