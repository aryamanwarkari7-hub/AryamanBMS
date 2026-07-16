using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AryamanBMS.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
