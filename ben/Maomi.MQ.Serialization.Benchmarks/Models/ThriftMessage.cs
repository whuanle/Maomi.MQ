using Thrift.Protocol;
using Thrift.Protocol.Entities;

namespace Maomi.MQ.Serialization.Benchmarks.Models;

public sealed class ThriftMessage : TBase
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public async Task ReadAsync(TProtocol protocol, CancellationToken cancellationToken)
    {
        await protocol.ReadStructBeginAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            var field = await protocol.ReadFieldBeginAsync(cancellationToken).ConfigureAwait(false);
            if (field.Type == TType.Stop)
            {
                break;
            }

            switch (field.ID)
            {
                case 1 when field.Type == TType.I32:
                    Id = await protocol.ReadI32Async(cancellationToken).ConfigureAwait(false);
                    break;
                case 2 when field.Type == TType.String:
                    Name = await protocol.ReadStringAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case 3 when field.Type == TType.String:
                    Email = await protocol.ReadStringAsync(cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await SkipFieldAsync(protocol, field.Type, cancellationToken).ConfigureAwait(false);
                    break;
            }

            await protocol.ReadFieldEndAsync(cancellationToken).ConfigureAwait(false);
        }

        await protocol.ReadStructEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(TProtocol protocol, CancellationToken cancellationToken)
    {
        await protocol.WriteStructBeginAsync(new TStruct(nameof(ThriftMessage)), cancellationToken).ConfigureAwait(false);

        await protocol.WriteFieldBeginAsync(new TField(nameof(Id), TType.I32, 1), cancellationToken).ConfigureAwait(false);
        await protocol.WriteI32Async(Id, cancellationToken).ConfigureAwait(false);
        await protocol.WriteFieldEndAsync(cancellationToken).ConfigureAwait(false);

        await protocol.WriteFieldBeginAsync(new TField(nameof(Name), TType.String, 2), cancellationToken).ConfigureAwait(false);
        await protocol.WriteStringAsync(Name, cancellationToken).ConfigureAwait(false);
        await protocol.WriteFieldEndAsync(cancellationToken).ConfigureAwait(false);

        await protocol.WriteFieldBeginAsync(new TField(nameof(Email), TType.String, 3), cancellationToken).ConfigureAwait(false);
        await protocol.WriteStringAsync(Email, cancellationToken).ConfigureAwait(false);
        await protocol.WriteFieldEndAsync(cancellationToken).ConfigureAwait(false);

        await protocol.WriteFieldStopAsync(cancellationToken).ConfigureAwait(false);
        await protocol.WriteStructEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Task SkipFieldAsync(TProtocol protocol, TType fieldType, CancellationToken cancellationToken)
    {
        return fieldType switch
        {
            TType.Bool => protocol.ReadBoolAsync(cancellationToken).AsTask(),
            TType.Byte => protocol.ReadByteAsync(cancellationToken).AsTask(),
            TType.Double => protocol.ReadDoubleAsync(cancellationToken).AsTask(),
            TType.I16 => protocol.ReadI16Async(cancellationToken).AsTask(),
            TType.I32 => protocol.ReadI32Async(cancellationToken).AsTask(),
            TType.I64 => protocol.ReadI64Async(cancellationToken).AsTask(),
            TType.String => protocol.ReadStringAsync(cancellationToken).AsTask(),
            _ => throw new NotSupportedException($"Field type '{fieldType}' is not supported by benchmark model."),
        };
    }
}
