namespace Maomi.MQ;

public enum FallbackState
{
    /// <summary>
    /// 顺利完成，直接 ACK.
    /// </summary>
    Ack = 1,

    /// <summary>
    /// 失败，失败后使用默认的配置.
    /// </summary>
    Nack = 1 << 1,

    /// <summary>
    /// 失败，失败后重试.
    /// </summary>
    NackAndRetry = 1 << 2,

    /// <summary>
    /// 失败，失败后不重试.
    /// </summary>
    NackNotRetry = 1 << 3
}
