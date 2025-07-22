namespace MySensorApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Login { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
