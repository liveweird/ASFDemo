using System;
using Microsoft.AspNet.SignalR;
using ASFDemo.CellActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Actors.Client;

namespace ASFDemo.Web.Hubs
{
    public class CellularHub : Hub
    {
        private readonly Uri _ecoUrl = new Uri("fabric:/ASFDemo/EcosystemActorService");

        public void RandomWakeUp(int cnt)
        {
            var random = new Random((int)DateTime.Now.Ticks);
            for (var i = 0; i < cnt; i++)
            {
                var eco = ActorProxy.Create<IEcosystemActor>(new ActorId("ecosystem"), _ecoUrl);
                eco.WakeUpCell(random.Next(10), random.Next(10));
            }
        }

        public void WakeMeUp(int x, int y)
        {
            var eco = ActorProxy.Create<IEcosystemActor>(new ActorId("ecosystem"), _ecoUrl);
            eco.WakeUpCell(x, y);
        }
    }

    class EcosystemEventsHandler : IEcosystemEvents
    {
        private readonly IApplicationBuilder _builder;

        public EcosystemEventsHandler(IApplicationBuilder builder)
        {
            _builder = builder;
        }

        public void CellUpdated(int x, int y, bool isAlive)
        {
            var context = _builder.ApplicationServices.GetRequiredService<IHubContext<CellularHub>>();
            context.Clients.All.addCellChange(x, y, isAlive ? 128 : 0);
        }
    }
}
