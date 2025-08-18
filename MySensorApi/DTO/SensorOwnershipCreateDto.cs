namespace MySensorApi.DTO
{
    public class SensorOwnershipCreateDto
    {
        public int UserId { get; set; }
        public string ChipId { get; set; } = null!;
        public string RoomName { get; set; } = null!;
        public string ImageName { get; set; } = null!;
    }
}
