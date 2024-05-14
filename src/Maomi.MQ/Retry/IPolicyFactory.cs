using Polly.Retry;

namespace Maomi.MQ.Retry
{
    // 重试策略提供器
    //  重试策略持久化器
    // 消息唯一 id

    public interface IPolicyFactory
    {
        AsyncRetryPolicy CreatePolicy(string queue);
    }
}
