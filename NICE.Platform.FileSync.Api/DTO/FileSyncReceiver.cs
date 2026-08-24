namespace NICE.Platform.FileSync.Api.DTO;

public class FileSyncReceiver
{
    public string ConnectionId { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    //name of the pipe to connect to
    public string PipeName { get; set; } = string.Empty;
}
