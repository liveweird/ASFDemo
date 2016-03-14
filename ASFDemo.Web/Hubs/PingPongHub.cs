using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Sandbox.Hubs
{
    [HubName("pingpong")]
    public class PingPongHub : Hub
    {
        public void Ping(int msg)
        {
            Clients.All.Pong(msg + 1);
        }
    }
}