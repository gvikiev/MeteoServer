namespace MySensorApi.DTO
{
    public class RoomWithSensorDto
    {
        public int Id { get; set; }
        public string RoomName { get; set; } = default!;
        public string ImageName { get; set; } = default!;
        public float? Temperature { get; set; }
        public float? Humidity { get; set; }
    }

}
