using Maomi.MQ;

namespace PublisherWeb.MQ;

[Consumer("publish_event", Qos = 1, RetryFaildRequeue = true)]
public class TranConsumer : TestEventConsumer
{
}
