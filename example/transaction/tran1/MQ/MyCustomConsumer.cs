using Maomi.MQ;
using Maomi.MQ.Transaction;

namespace tran1.MQ;

[Consumer("tran_p1")]
public class MyCustomConsumer : IDbTransactionConsumer<CreateTestEntityMessage>
{
    private static int _retryCount = 0;

    public Task ExecuteAsync(MessageHeader messageHeader, CreateTestEntityMessage message)
    {
        throw new NotImplementedException();
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, CreateTestEntityMessage message)
    {
        _retryCount++;
        return Task.CompletedTask;
    }

    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, CreateTestEntityMessage? message, Exception? ex)
    {
        await Task.CompletedTask;

        if (_retryCount > 10)
        {
            return ConsumerState.NackAndNoRequeue;
        }

        return ConsumerState.NackAndRequeue;
    }
}