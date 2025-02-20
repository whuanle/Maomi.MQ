using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;

[Consumer("consumerWeb_dead", Expiration = 6000, DeadRoutingKey = "ConsumerWeb_dead_queue")]
public class EmptyDeadConsumer : EmptyConsumer<DeadEvent>
{
}
