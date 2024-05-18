namespace Maomi.MQ;

/// <summary>
/// 序列化消息.
/// </summary>
public interface IJsonSerializer
{
    /// <summary>
    /// Serializer.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <param name="obj">Object.</param>
    /// <returns><see cref="byte"/>[].</returns>
    public byte[] Serializer<TObject>(TObject obj)
        where TObject : class;

    /// <summary>
    /// Deserialize.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <param name="bytes"><see cref="byte"/>[].</param>
    /// <returns>TObject.</returns>
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
        where TObject : class;
}
