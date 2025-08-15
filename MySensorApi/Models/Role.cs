namespace MySensorApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();
    }

}
