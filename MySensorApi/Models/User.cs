using System.Data;

namespace MySensorApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public int RoleId { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; } = null!;
        public ICollection<SensorOwnership> SensorOwnerships { get; set; } = new List<SensorOwnership>();
    }
}
