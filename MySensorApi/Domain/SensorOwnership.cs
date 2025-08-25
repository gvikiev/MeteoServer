namespace MySensorApi.Models
{
    public class SensorOwnership
    {
        public int Id { get; set; }

        public User User { get; set; } = null!;

        public string ChipId { get; set; } = null!;

        public int UserId { get; set; }

        public string RoomName { get; set; } = null!;

        public string ImageName { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long Version { get; set; } = 1;                 // інкремент при кожній зміні
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // оновлюється при зміні
    }
}
