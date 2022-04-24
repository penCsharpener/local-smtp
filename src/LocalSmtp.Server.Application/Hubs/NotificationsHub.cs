using Microsoft.AspNetCore.SignalR;

namespace LocalSmtp.Server.Application.Hubs;

public class NotificationsHub : Hub
{
    public async Task OnMessagesChanged()
    {
        if (Clients != null)
        {
            await Clients.All.SendAsync("messageschanged");
        }
    }

    public async Task OnServerChanged()
    {
        if (Clients != null)
        {
            await Clients.All.SendAsync("serverchanged");
        }
    }

    public async Task OnSessionsChanged()
    {
        if (Clients != null)
        {
            await Clients.All.SendAsync("sessionschanged");
        }
    }
}
