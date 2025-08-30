namespace MySensorApi.DTO
{
    public class SensorOwnershipDto
    {
        public int? UserId { get; set; }                 // required для Create
        public string ChipId { get; set; } = null!;      // required завжди
        public string? RoomName { get; set; }            // optional для Update
        public string? ImageName { get; set; }           // optional для Update
    }
}
