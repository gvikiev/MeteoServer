using System.ComponentModel.DataAnnotations;

namespace MySensorApi.DTO
{
    public class UserAuthRequestDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Optional для login, required для register (перевіряємо в коді)
        public string? Email { get; set; }
    }
}
