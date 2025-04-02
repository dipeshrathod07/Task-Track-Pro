using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string? Email { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        public override string ToString()
        {
            return $"LoginVM {{ Email: {Email ?? "N/A"}, Password: {Password} }}";
        }
    }
}