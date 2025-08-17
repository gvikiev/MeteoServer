namespace MySensorApi.DTO
{
    public class SensorOwnershipRequestDTO
    {
        public string Username { get; set; } = null!;
        public string ChipId { get; set; } = default!;
        public string RoomName { get; set; } = default!;
        public string ImageName { get; set; } = default!;
    }
}
