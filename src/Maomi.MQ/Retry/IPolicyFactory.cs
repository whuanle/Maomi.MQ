using Polly.Retry;

namespace Maomi.MQ.Retry
{
    /// <summary>
    /// 重试策略工厂.
    /// </summary>
    public interface IPolicyFactory
    {
        /// <summary>
        /// 创建策略.
        /// </summary>
        /// <param name="queue">队列名称</param>
        /// <returns></returns>
        AsyncRetryPolicy CreatePolicy(string queue);
    }
}
