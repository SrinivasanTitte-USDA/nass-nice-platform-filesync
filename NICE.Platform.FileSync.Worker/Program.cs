using System.CommandLine;
using System.IO.Pipes;

Console.WriteLine("NASS Platform File Sync Worker");

//https://www.nuget.org/packages/System.CommandLine

var rootCommand = new RootCommand("NICE.Platform.FileSync.Service");

string clientId = string.Empty;
string pipeName = string.Empty;
var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
//var _fileLogger = new FileLogger();
const string logPath = @"C:\logs\dev\FileSyncSvc\FileSyncLog-2026-08-24.txt";
File.AppendAllText(logPath, $"NASS Platform File Sync Worker started at {DateTime.UtcNow:R}{Environment.NewLine}");


Option<string> clientIdOption = new("--clientId", "-c")
{
    Description = "The unique id of the connected client",
    Required = true
};

Option<string> pipeOption = new("--pipe", "-p")
{
    Description = "The name of the (named) pipe to connect to",
    Required = true
};

rootCommand.Options.Add(clientIdOption);
rootCommand.Options.Add(pipeOption);

rootCommand.SetAction(parseResult =>
{
    if (parseResult.Errors.Count > 0)
    {
        File.AppendAllText(logPath, $"Error - NASS Platform File Sync: invalid arguments.{Environment.NewLine}");
        foreach (var error in parseResult.Errors)
        {
            File.AppendAllText(logPath, $"{error.Message}{Environment.NewLine}");
        }
        return;
    }

    var parsedClientId = parseResult.GetValue(clientIdOption);
    clientId = parsedClientId!;

    var parsedPipeName = parseResult.GetValue(pipeOption);
    pipeName = parsedPipeName!;

});

var parseResult = rootCommand.Parse(args).Invoke();

//note: PipeTransmissionMode.Message is only supported on Windows, and not on Linux or MacOS. If you need to support those platforms, you may need to use PipeTransmissionMode.Byte instead, but that would require a different approach to message framing and parsing.

using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

File.AppendAllText(logPath, $"[Child] Waiting for parent process to connect...{Environment.NewLine}");
await pipeServer.WaitForConnectionAsync();
File.AppendAllText(logPath, $"[Child] Connected to parent. Listening for messages...{Environment.NewLine}");

//using var reader = new StreamReader(pipeServer);

using var streamWriter = new StreamWriter(pipeServer) { AutoFlush = true };
var fuBar = DateTime.UtcNow.ToString("R");

File.AppendAllText(logPath, $"[Child] writing connection time to the pipe...{Environment.NewLine}");

streamWriter.WriteLine($"Connected at '{fuBar}'");

#region read/write
//using var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true);
//using var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true);

//var command = await reader.ReadLineAsync(cancellationToken);

//if (command == "QUIT")
//{
//    cancellationTokenSource.Cancel();
//    //break;
//}

//// 2. Process command and get a result string
//string response = ProcessCommand(command);

//// 3. Write response back to parent
//await writer.WriteLineAsync(response);
//await writer.FlushAsync(cancellationToken);
#endregion


try
{
    // Block and wait for messages indefinitely until parent disconnects or terminates us
    //while (true)
    //{
    //    string? message = await reader.ReadLineAsync();

    //    if (message == null)
    //    {
    //        // If ReadLineAsync returns null, the pipe was closed/disconnected by the parent
    //        Console.WriteLine("[Child] Parent disconnected. Exiting.");
    //        break;
    //    }

    //    if (message.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    //    {
    //        Console.WriteLine("[Child] Termination command received from parent.");
    //        break;
    //    }

    //    ProcessMessage(message);
    //}
}
catch (Exception ex)
{
    Console.WriteLine($"[Child] Error in pipe communication: {ex.Message}");
}

static void ProcessMessage(string message)
{
    Console.WriteLine($"[Child] Received message: {message}");
    // Perform work based on message content here...
}
Console.WriteLine("Hello, World!");
