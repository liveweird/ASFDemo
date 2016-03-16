using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace ASFDemo.CellActor.Interfaces
{
    /// <summary>
    /// This interface represents the actions a client app can perform on an actor.
    /// It MUST derive from IActor and all methods MUST return a Task.
    /// </summary>

    public interface IEcosystemEvents : IActorEvents
    {
        void CellUpdated(int x, int y, bool isAlive);
    }

    public interface IEcosystemActor : IActor, IActorEventPublisher<IEcosystemEvents>
    {
        Task Initialize();
        Task WakeUpCell(int x, int y);
        Task Refresh(object state);
        Task NotifyCellIsAlive(int x, int y);
        Task NotifyCellHasDied(int x, int y);
    }

    public interface ICellActor : IActor
    {
        Task Initialize(int x, int y, string[] neighbors);
        Task WakeUp();
        Task Refresh();
        Task NeighborIsAlive();
        Task NeighborHasDied();
    }
}
