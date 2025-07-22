namespace MySensorApi.Models
{
    public class UserRegistrationDto
    {
        public string? Login { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
    }
}
