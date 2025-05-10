using Microsoft.AspNetCore.SignalR;
using AstrologyApp.Models;
using Microsoft.EntityFrameworkCore;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(ChatMessage chatMessage)
    {
        chatMessage.Timestamp = DateTime.UtcNow;

        // Save message to database
        _context.Messages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Broadcast message to connected clients
        await Clients.All.SendAsync("ReceiveMessage", chatMessage);
    }

    public async Task<List<ChatMessage>> GetMessages(string sender, string receiver)
    {
        return await _context.Messages
            .Where(m => (m.Sender == sender && m.Receiver == receiver) || (m.Sender == receiver && m.Receiver == sender))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
}
