using NICE.Platform.FileSync.Api.DTO;
using System.Diagnostics;

namespace NICE.Platform.FileSync.Api.Hubs;

public class ConnectionTracker
{
    private readonly HashSet<FileSyncReceiver> _receivers = [];
    public void AddReceiver(FileSyncReceiver receiver)
    {
        //TODO - protect using a lock or concurrent collection
        _receivers.Add(receiver);
    }
    public void RemoveReceiverByIPV4Address(string ipv4Address)
    {
        //find the spawned process that is connected to the hub via the named pipe and remove it from the list of receivers
        foreach (var processId in _receivers.Where(r => r.IPAddress == ipv4Address).Select(r => r.ProcessId))
        {
            try
            {
                // Associate the Process component with the identifier
                using var process = Process.GetProcessById(processId);

                // Check if the process is still running
                if (!process.HasExited)
                {
                    // Pass 'true' to also terminate any spawned child processes
                    process.Kill(entireProcessTree: true);

                    // Wait briefly to ensure it has fully shut down
                    process.WaitForExit();
                    Console.WriteLine($"Process {processId} was terminated successfully.");
                }
                else
                {
                    Console.WriteLine($"Process {processId} is no longer active.");
                }
            }
            catch (ArgumentException)
            {
                // Thrown if no process with the specified ID is currently running
                Console.WriteLine($"No active process found with ID {processId}.");
            }
            catch (InvalidOperationException)
            {
                // Thrown if the process has already exited or access is denied
                Console.WriteLine($"Unable to terminate process {processId}; it may have already exited or access was denied.");
            }
        }

        _receivers.RemoveWhere(r => r.IPAddress == ipv4Address);
    }
}
