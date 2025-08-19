namespace MySensorApi.DTO
{
    public class SensorOwnershipUpdateDto
    {
        public string ChipId { get; set; } = null!;
        public string? RoomName { get; set; }
        public string? ImageName { get; set; }
    }
}
