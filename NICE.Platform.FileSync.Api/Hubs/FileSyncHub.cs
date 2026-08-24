using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NICE.Platform.FileSync.Api.DTO;
using System.Diagnostics;
using System.IO.Pipes;

namespace NICE.Platform.FileSync.Api.Hubs;

[Authorize]
public class FileSyncHub : Hub
{
    //connection/session tracker
    private readonly ConnectionTracker _connectionTracker;

    public FileSyncHub(ConnectionTracker connectionTracker)
    {
        _connectionTracker = connectionTracker;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId.ToString();
        if (string.IsNullOrEmpty(connectionId))
        {
            Debug.WriteLine("ConnectionId is null or empty.");
            return;
        }
        var ipv4Address = Context.GetHttpContext()?.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        if (string.IsNullOrEmpty(ipv4Address))
        {
            Debug.WriteLine("IPv4 address is null or empty.");
            return;
        }

        var pipeName = $"pipe-file-sync-svc-{connectionId}";

        //create named pipe for the spawned process to connect to the hub
        using var pipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        Debug.WriteLine($"Created named pipe client stream for pipe: {pipeName}");

        var cancellationToken = Context.ConnectionAborted;
        //setup listener for named pipe connection
        await ListenToChildAsync(connectionId, pipeClientStream, cancellationToken);

        Debug.WriteLine($"Finished listening to child for pipe: {pipeName}");

        var workerPath = @"C:\usdadev\nice\nass-nice-platform-filesync\NICE.Platform.FileSync.Worker\bin\Release\net10.0\publish\NICE.Platform.FileSync.Worker.exe";

        //create child process
        var startInfo = new ProcessStartInfo
        {
            FileName = workerPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add named arguments and their values sequentially
        startInfo.ArgumentList.Add("--clientId");
        startInfo.ArgumentList.Add(connectionId);

        startInfo.ArgumentList.Add("--pipe");
        startInfo.ArgumentList.Add(pipeName); // Safely handles spaces

        //startInfo.ArgumentList.Add("--arg3");
        //startInfo.ArgumentList.Add("value3");

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            Debug.WriteLine("Failed to start the child process.");
            return;
        }

        Debug.WriteLine($"Started child process with PID: {process.Id}");

        var receiver = new FileSyncReceiver
        {
            ConnectionId = connectionId,
            IPAddress = ipv4Address,
            //note - follow a consistent naming convention for the pipe name to avoid conflicts and ensure uniqueness
            //pipe names are also lowercase for linux portability
            PipeName = pipeName,
            ProcessId = process.Id
        };
        //only one receiver per ipv4 address is allowed since receivers don't have unique identifiers or user contexts, so remove any existing receiver with the same ipv4 address
        _connectionTracker.RemoveReceiverByIPV4Address(ipv4Address);
        _connectionTracker.AddReceiver(receiver);

        await base.OnConnectedAsync();
    }

    private async Task ListenToChildAsync(string connectionId, NamedPipeClientStream pipeStream, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException();
        var buffer = new byte[4096];
        try
        {
            // Use message mode or frame your payloads properly
            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                // Asynchronously read data sent from the child worker
                int bytesRead = await pipeStream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    // End of stream / Client disconnected
                    break;
                }

                ProcessChildMessage(connectionId, buffer.AsMemory(0, bytesRead));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Handle unexpected drop or I/O fault
            //LogError(childId, ex);
        }
        finally
        {
            //_activeChildren.TryRemove(childId, out _);
            await pipeStream.DisposeAsync();
        }

    }

    private void ProcessChildMessage(string connectionId, ReadOnlyMemory<byte> memory)
    {
        //throw new NotImplementedException();
        Debug.WriteLine($"Received message from child for connection {connectionId}: {System.Text.Encoding.UTF8.GetString(memory.Span)}");
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var ipv4Address = Context.GetHttpContext()?.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        await base.OnDisconnectedAsync(exception);
    }
}
