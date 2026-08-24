using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Net.Http.Json;

namespace NICE.Platform.FileSync.Forms;

record TokenResponse(string token);
public partial class Form1 : Form
{
    private HubConnection? _hubConnection;
    public Form1()
    {
        InitializeComponent();
    }

    private async void button1_Click(object sender, EventArgs e)
    {
        //var cert = new X509Certificate2(Path.Combine(_environment.ContentRootPath, "sts_dev_cert.pfx"), "1234");

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

        connection.On<string>("ReceiveMessage", (message) =>
        {
            MessageBox.Show($"Received message: {message}");
        });
        await connection.StartAsync();
    }

    private async void button2_Click(object sender, EventArgs e)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            Debug.WriteLine("Hub connection is not established, or already closed.");
            return;
        }
        await _hubConnection.StopAsync();
    }
}
