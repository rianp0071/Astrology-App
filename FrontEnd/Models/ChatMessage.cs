namespace AstrologyApp.Models
{
    public class ChatMessage
    {
        public string Sender { get; set; } = string.Empty; // Who sent the message
        public string Receiver { get; set; } = string.Empty; // Who is receiving the message
        public string Message { get; set; } = string.Empty; // Message content
        public DateTime Timestamp { get; set; } // Time the message was sent
    }
}
