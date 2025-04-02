using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class User
    {
        public Guid? UserId { get; set; }

        public char Role { get; set; } = 'E';

        [Required(ErrorMessage = "First name is required")]
        [StringLength(255)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(255)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        public string? Address { get; set; }

        [StringLength(15)]
        public string? Contact { get; set; }

        public char? Gender { get; set; }

        public string? Image { get; set; }
        public IFormFile? ImageFile { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public override string ToString()
        {
            return $"User {{ ID: {UserId}, Role: {Role}, Name: {FirstName} {LastName}, " +
                   $"Email: {Email}, Address: {Address}, Contact: {Contact}, " +
                   $"Gender: {Gender}, Image: {Image}, Created: {CreatedAt}, Updated: {UpdatedAt} }}";
        }
    }
}