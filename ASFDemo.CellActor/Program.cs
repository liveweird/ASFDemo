using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ASFDemo.CellActor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<EcosystemActor>()
                            .Wait();
                ActorRuntime.RegisterActorAsync<CellActor>()
                            .Wait();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}
