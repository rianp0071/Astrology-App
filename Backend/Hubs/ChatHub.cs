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

        if (chatMessage.Receiver == "Public")
        {
            // 🚀 Broadcast public messages to ALL users
            await Clients.All.SendAsync("ReceiveMessage", chatMessage);
        }
        else
        {
            // 🔹 Send private messages ONLY to sender & receiver
            await Clients.User(chatMessage.Receiver).SendAsync("ReceiveMessage", chatMessage);
            await Clients.User(chatMessage.Sender).SendAsync("ReceiveMessage", chatMessage);
        }
    }

    public async Task<List<ChatMessage>> GetMessages(string sender, string receiver)
    {
        return await _context.Messages
            .Where(m => (m.Sender == sender && m.Receiver == receiver) || (m.Sender == receiver && m.Receiver == sender))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }
}
