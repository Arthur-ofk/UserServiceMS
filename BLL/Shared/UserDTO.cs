using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Shared
{
    public class UserDTO
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "UserName is required.")]
        [MaxLength(50)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string PasswordHash { get; set; } 
        public string Role { get; set; }
    }
}
