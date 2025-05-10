using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using AstrologyApp.Models;

public class ChatService
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;

    public event Action<ChatMessage>? OnMessageReceived;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;


    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5042/chatHub") // Backend SignalR URL
            .Build();

        _hubConnection.On<ChatMessage>("ReceiveMessage", (chatMessage) =>
        {
            OnMessageReceived?.Invoke(chatMessage);
        });

        await _hubConnection.StartAsync();
    }

    public async Task SendMessage(ChatMessage chatMessage)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("SendMessage", chatMessage);
        }
    }

    // Fetch previous messages between two users
    public async Task<List<ChatMessage>> GetMessages(string sender, string receiver)
    {
        var response = await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"http://localhost:5042/getMessages/{sender}/{receiver}");
        return response ?? new List<ChatMessage>();
    }
}
