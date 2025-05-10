using System.ComponentModel.DataAnnotations;

namespace AstrologyApp.Models
{
    public class ChatMessage
    {
        [Key] // Primary key for database storage
        public int Id { get; set; }

        [Required]
        public string Sender { get; set; } = string.Empty; // The user sending the message

        [Required]
        public string Receiver { get; set; } = string.Empty; // The user receiving the message

        [Required]
        public string Message { get; set; } = string.Empty; // The actual message content

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Auto-set timestamp when sent
    }
}
