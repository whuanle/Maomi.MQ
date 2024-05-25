using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Global configuration. <br />
/// 全局配置.
/// </summary>
public class MqOptions
{
    ///// <summary>
    ///// Application name.<br />
    ///// 应用名称.
    ///// </summary>
    //public string ApplicationName { get; set; } = Assembly.GetEntryAssembly()?.GetName()?.Name!;

    ///// <summary>
    ///// Application version.<br />
    ///// 应用版本号.
    ///// </summary>
    //public string ApplicationVersion { get; set; } = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// 0-1024.
    /// </summary>
    public int WorkId { get; set; }

    /// <summary>
    /// Create queues on startup,<see cref="RabbitMQ.Client.IChannel.QueueDeclareAsync"/>.<br />
    /// 是否自动创建队列.
    /// </summary>
    public bool AutoQueueDeclare { get; set; } = true;
}
