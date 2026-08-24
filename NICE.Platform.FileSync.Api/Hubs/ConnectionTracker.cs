using NICE.Platform.FileSync.Api.DTO;

namespace NICE.Platform.FileSync.Api.Hubs;

public class ConnectionTracker
{
    private readonly HashSet<FileSyncReceiver> _receivers = [];
    public void AddReceiver(FileSyncReceiver receiver)
    {
        _receivers.Add(receiver);
    }
    public void RemoveReceiverByIPV4Address(string ipv4Address)
    {
        var receiver = _receivers.FirstOrDefault(r => r.IPAddress == ipv4Address);
        if (receiver != null)
        {
            _receivers.Remove(receiver);
        }
    }
}
