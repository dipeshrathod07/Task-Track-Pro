using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class Task
    {
        public Guid? TaskId { get; set; }
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public int? EstimatedDays { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(15)]
        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }

        public override string ToString()
        {
            return $"Task {{ ID: {TaskId}, User ID: {UserId}, Title: {Title}, " +
                   $"Description: {Description ?? "N/A"}, Estimated Days: {EstimatedDays}, " +
                   $"Start Date: {StartDate}, End Date: {EndDate}, " +
                   $"Status: {Status}, Created: {CreatedAt}, Updated: {UpdatedAt} }}";
        }
    }
}