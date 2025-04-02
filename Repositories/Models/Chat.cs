using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class Chat
    {
        [Key]
        public int? ChatId { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = "Hello";

        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public string? SenderName { get; set; }
        public string? ReceiverName { get; set; }
    }
}