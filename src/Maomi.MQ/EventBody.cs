namespace Maomi.MQ
{
    public class EventBody<TEvent>
    {
        public long Id { get; init; }
        public DateTimeOffset CreateTime { get; init; }
        public TEvent Body { get; init; }
    }
}
