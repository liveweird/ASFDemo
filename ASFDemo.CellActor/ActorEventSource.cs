using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace ASFDemo.CellActor
{
    [EventSource(Name = "MyCompany-ASFDemo-ASFDemo.CellActor")]
    internal sealed class ActorEventSource : EventSource
    {
        public static readonly ActorEventSource Current = new ActorEventSource();

        static ActorEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { })
                .Wait();
        }

        private ActorEventSource() : base()
        {
        }

        public static class Keywords
        {
            public const EventKeywords HostInitialization = (EventKeywords) 0x1L;
        }

        [NonEvent]
        public void Message(string message,
                            params object[] args)
        {
            var finalMessage = string.Format(message,
                                             args);
            this.Message(finalMessage);
        }

        [Event(1, Level = EventLevel.Verbose)]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(1,
                                message);
            }
        }

        [NonEvent]
        public void ActorHostInitializationFailed(Exception e)
        {
            this.ActorHostInitializationFailed(e.ToString());
        }

        [Event(101, Level = EventLevel.Error, Message = "Actor host initialization failed")]
        private void ActorHostInitializationFailed(string exception)
        {
            this.WriteEvent(3, exception);
        }
    }
}
