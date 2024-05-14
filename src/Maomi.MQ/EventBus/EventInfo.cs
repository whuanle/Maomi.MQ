namespace Maomi.MQ.EventBus
{
    public class EventInfo
    {
        public int Qos { get; internal set; }
        public string Queue { get; internal set; }
        public Type EventType { get; internal set; }
        public Type Middleware { get; internal set; }

        public string? Group { get; set; }

        public bool Requeue { get; set; }
        public Dictionary<int, Type> Handlers { get; internal set; }
    }
}
