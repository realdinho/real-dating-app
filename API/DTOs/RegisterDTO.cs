using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class RegisterDTO
    {
        [Required]
        public string username { get; set; }

        [Required]        
        public string password { get; set; }
    }
}