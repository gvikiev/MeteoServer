using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySensorApi.Models
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;

        public ICollection<User> Users { get; set; } = new List<User>();
    }

}
