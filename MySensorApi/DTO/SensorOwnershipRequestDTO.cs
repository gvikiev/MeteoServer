namespace MySensorApi.DTO
{
    public class SensorOwnershipRequestDTO
    {
        public int UserId { get; set; }
        public string ChipId { get; set; } = default!;
        public string RoomName { get; set; } = default!;
        public string ImageName { get; set; } = default!;
    }
}
