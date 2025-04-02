using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class ChangePasswordVM
    {
        [Required(ErrorMessage = "UserId is required")]
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "Old Password is required")]
        public string? OldPassword { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("NewPassword", ErrorMessage = "Confirm Password does not match New Password")]
        public string? ConfirmPassword { get; set; }
    }
}