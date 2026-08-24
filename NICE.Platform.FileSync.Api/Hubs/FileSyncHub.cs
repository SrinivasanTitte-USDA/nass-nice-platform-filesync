using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NICE.Platform.FileSync.Api.Hubs;

[Authorize]
public class FileSyncHub : Hub
{
}
