using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using AstrologyApp.Models;

public class ChatService
{
    private HubConnection? _hubConnection;
    private readonly HttpClient _httpClient;
    private bool _isEventSubscribed = false; // ✅ Prevent duplicate subscriptions

    public event Action<ChatMessage>? OnMessageReceived;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection == null)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5042/chatHub")
                .Build();
        }

        if (!_isEventSubscribed) 
        {
            _hubConnection.On<ChatMessage>("ReceiveMessage", chatMessage =>
            {
                Console.WriteLine($"SignalR Received: {chatMessage.Message}");
                OnMessageReceived?.Invoke(chatMessage);
            });

            _isEventSubscribed = true;
        }

        await _hubConnection.StartAsync();
    }


    public async Task SendMessage(ChatMessage chatMessage)
    {
        Console.WriteLine($"Sending message: {chatMessage.Message}");
        if (IsConnected)
        {
            Console.WriteLine($"Sending message: hub connection is connected");
            if (_hubConnection != null)
            {
                Console.WriteLine($"Sending message: hub is not null");
                await _hubConnection.SendAsync("SendMessage", chatMessage);
            }
        }
    }

    public async Task<List<ChatMessage>> GetMessages(string sender, string receiver)
    {
        var response = await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"http://localhost:5042/getMessages/{sender}/{receiver}");
        return response ?? new List<ChatMessage>();
    }

    public async Task Disconnect()
    {
        if (IsConnected)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                _isEventSubscribed = false; // ✅ Reset flag when disconnecting
            }
        }
    }
}
