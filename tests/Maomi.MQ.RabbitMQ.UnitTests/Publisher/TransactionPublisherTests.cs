using Maomi.MQ;
using Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maomi.MQ.RabbitMQ.UnitTests.Publisher;

public class TransactionPublisherTests
{
    [Fact]
    public async Task TxMethods_ShouldDelegateToChannel()
    {
        var harness = new RabbitMqTestHarness();
        var dedicated = harness.CreateAdditionalChannel("tx-ctag");

        var provider = harness.BuildProvider();
        var basePublisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        var transaction = basePublisher.CreateTransaction();

        await transaction.TxSelectAsync();
        await transaction.TxCommitAsync();
        await transaction.TxRollbackAsync();

        dedicated.Verify(x => x.TxSelectAsync(It.IsAny<CancellationToken>()), Times.Once);
        dedicated.Verify(x => x.TxCommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        dedicated.Verify(x => x.TxRollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
