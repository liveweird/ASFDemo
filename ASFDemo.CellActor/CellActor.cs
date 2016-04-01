using ASFDemo.CellActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ASFDemo.CellActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class EcosystemActor : Actor, IEcosystemActor
    {
        private IActorTimer _refreshTimer;
        public const int Size = 10;
        private readonly Uri _cellUrl = new Uri("fabric:/ASFDemo/CellActorService");

        [DataContract]
        internal sealed class ActorState
        {
        }

        protected override Task OnActivateAsync()
        {
            _refreshTimer = RegisterTimer(Refresh, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            if (_refreshTimer != null)
            {
                UnregisterTimer(_refreshTimer);
            }

            return base.OnDeactivateAsync();
        }

        private string GenId(int x, int y)
        {
            return string.Format("{0}_{1}", x, y);
        }

        private ICellActor GetCell(int x, int y)
        {
            return ActorProxy.Create<ICellActor>(new ActorId(GenId(x, y)), _cellUrl);
        }

        public Task NotifyCellHasDied(int x, int y)
        {
            var ev = GetEvent<IEcosystemEvents>();
            ev.CellUpdated(x, y, false);

            return Task.FromResult(true);
        }

        public Task NotifyCellIsAlive(int x, int y)
        {
            var ev = GetEvent<IEcosystemEvents>();
            ev.CellUpdated(x, y, true);

            return Task.FromResult(true);
        }

        public Task Initialize()
        {
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    var _x = (x >= 1) ? x - 1 : Size - 1;
                    var x_ = (x + 1 < Size) ? x + 1 : 0;
                    var _y = (y >= 1) ? y - 1 : Size - 1;
                    var y_ = (y + 1 < Size) ? y + 1 : 0;

                    ICellActor cell = GetCell(x, y);
                    cell.Initialize(x, y, new[]
                                                {
                                                    GenId(_x, _y), GenId(_x, y), GenId(_x, y_),
                                                    GenId(x, _y), GenId(x, y_),
                                                    GenId(x_, _y), GenId(x_, y), GenId(x_, y_),
                                                });
                }
            }

            return Task.FromResult(true);
        }

        public Task Refresh(object state)
        {
            ActorEventSource.Current.Message("Refreshing ecosystem");
            for (var x = 0; x < Size; x++)
            {
                for (var y = 0; y < Size; y++)
                {
                    ICellActor cell = GetCell(x, y);
                    cell.Refresh();
                }
            }

            return Task.FromResult(true);
        }

        public Task WakeUpCell(int x, int y)
        {
            ActorEventSource.Current.Message("Waking up a cell {0}_{1}", x, y);
            ICellActor cell = GetCell(x, y);
            return cell.WakeUp();
        }
    }

    [StatePersistence(StatePersistence.Persisted)]
    internal class CellActor : Actor, ICellActor
    {
        [DataContract]
        internal sealed class ActorState
        {
            [DataMember]
            public int X { get; set; }
            [DataMember]
            public int Y { get; set; }
            [DataMember]
            public string[] Neighbors { get; set; }
            [DataMember]
            public bool Alive { get; set; }
            [DataMember]
            public short NeighborsAlive { get; set; }
        }

        private readonly Uri _cellUrl = new Uri("fabric:/ASFDemo/CellActorService");
        private readonly Uri _ecoUrl = new Uri("fabric:/ASFDemo/EcosystemActorService");

        protected ActorState State
        {
            get
            {
                return this.StateManager.GetStateAsync<ActorState>("state")
                           .Result;
            }
            set
            {
                this.StateManager.SetStateAsync("state",
                                                value);
            }
        }

        protected override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                this.State = new ActorState { Alive = false, NeighborsAlive = 0, Neighbors = new string[] { }, X = -1, Y = -1 };
            }

            ActorEventSource.Current.Message("State initialized to {0}", this.State);
            return Task.FromResult(true);
        }

        private IEcosystemActor GetEco()
        {
            return ActorProxy.Create<IEcosystemActor>(new ActorId("ecosystem"), _ecoUrl);
        }

        private ICellActor GetCell(string neighbor)
        {
            return ActorProxy.Create<ICellActor>(new ActorId(neighbor), _cellUrl);
        }

        private void Die()
        {
            foreach (var neighbor in this.State.Neighbors)
            {
                GetCell(neighbor).NeighborHasDied();
            }

            this.State.Alive = false;
            ActorEventSource.Current.Message("Dying... {0}_{1}", this.State.X, this.State.Y);

            GetEco().NotifyCellHasDied(this.State.X, this.State.Y);
        }

        private void Live()
        {
            foreach (var neighbor in this.State.Neighbors)
            {
                GetCell(neighbor).NeighborIsAlive();
            }

            this.State.Alive = true;
            ActorEventSource.Current.Message("Raising... {0}_{1}", this.State.X, this.State.Y);

            GetEco().NotifyCellIsAlive(this.State.X, this.State.Y);
        }

        public Task Initialize(int x, int y, string[] neighbors)
        {
            this.State = new ActorState { Alive = false, Neighbors = (string[]) neighbors.Clone(), NeighborsAlive = 0, X = x, Y = y };
            return Task.FromResult(true);
        }

        public Task WakeUp()
        {
            if (!(this.State.Alive))
            {
                Live();
            }

            return Task.FromResult(true);
        }

        public Task Refresh()
        {
            if (this.State.Alive)
            {
                switch (this.State.NeighborsAlive)
                {
                    case 2:
                    case 3:
                        break;
                    default:
                        Die();
                        break;
                }
            }
            else
            {
                if (this.State.NeighborsAlive == 3)
                {
                    Live();
                }
            }

            return Task.FromResult(true);
        }

        public Task NeighborIsAlive()
        {
            this.State.NeighborsAlive += 1;
            return Task.FromResult(true);
        }

        public Task NeighborHasDied()
        {
            this.State.NeighborsAlive -= 1;
            return Task.FromResult(true);
        }
    }
}
