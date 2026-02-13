namespace Maomi.MQ.RabbitMQ.UnitTests.Contracts;

public class InterfaceContractSmokeTests
{
    [Fact]
    public void Interfaces_ShouldContainExpectedMembers()
    {
        Assert.Contains(typeof(IDynamicConsumer).GetMethods(), m => m.Name == "StopConsumerAsync");
        Assert.Contains(typeof(IDynamicConsumer).GetMethods(), m => m.Name == "StopConsumerTagAsync");

        Assert.Contains(typeof(IMessagePublisher).GetMethods(), m => m.Name == "AutoPublishAsync");
        Assert.Contains(typeof(IMessagePublisher).GetMethods(), m => m.Name == "PublishAsync");
        Assert.Contains(typeof(IMessagePublisher).GetMethods(), m => m.Name == "CustomPublishAsync");

        Assert.Contains(typeof(IChannelMessagePublisher).GetMethods(), m => m.Name == "PublishChannelAsync");

        Assert.Contains(typeof(ITransactionPublisher).GetMethods(), m => m.Name == "TxSelectAsync");
        Assert.Contains(typeof(ITransactionPublisher).GetMethods(), m => m.Name == "TxCommitAsync");
        Assert.Contains(typeof(ITransactionPublisher).GetMethods(), m => m.Name == "TxRollbackAsync");
    }
}
