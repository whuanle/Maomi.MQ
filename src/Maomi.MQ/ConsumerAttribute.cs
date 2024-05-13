using Polly;
using Polly.Retry;

namespace Maomi.MQ
{
    public class ConsumerAttribute : Attribute
    {
        private ushort _qos = 10;
        public ushort Qos
        {
            get => _qos;
            set
            {
                if (value <= 0)
                {
                    _qos = 1;
                }
                else
                {
                    _qos = value;
                }
            }
        }
    }

    public class DefaultAsyncRetryPolicy
    {
        public int RetryCount { get; init; }

    }

    // 重试策略提供器
    //  重试策略持久化器
    // 消息唯一 id

    public interface IPolicyFactory
    {
        AsyncRetryPolicy CreatePolicy(string queue);
    }

    public class DefaultPolicyFactory: IPolicyFactory
    {
        public AsyncRetryPolicy CreatePolicy(string queue)
        {
            // 创建异步重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            return retryPolicy;
        }
    }
}
