using Microsoft.AspNetCore.SignalR.Client;
using NICE.Platform.FileSync.Receiver;
using System.Diagnostics;
using System.Net.Http.Json;

Console.WriteLine($"Receiver process starting at {DateTime.Now}");
HubConnection? _hubConnection = null;
//record TokenResponse(string token);

while (true)
{
    var command = Console.ReadLine();
    if (command?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) == true || command?.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine("Quit command received. Exiting...");
        return;
        //break;
    }
    else if (command?.Trim().Equals("connect", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine("Connecting to SignalR hub...");
        var httpClient = new HttpClient();
        var response = await httpClient.GetFromJsonAsync<TokenResponse>("https://localhost:7221/api/v1/token");
        var token = response?.token;
        var connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7221/v1/hubs/FileSync", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        _hubConnection = connection;
        Console.WriteLine("Connected to SignalR hub.");

        connection.On<string>("ReceiveMessage", (message) =>
        {
            Console.WriteLine($"Received message: {message}");
        });
        await connection.StartAsync();
    }
    else if (command?.Trim().Equals("disconnect", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.WriteLine("Disconnecting from SignalR hub...");
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            Debug.WriteLine("Hub connection is not established, or already closed.");
            return;
        }
        await _hubConnection.StopAsync();
    }
    else
    {
        Console.WriteLine($"Received input: {command}");
    }
}