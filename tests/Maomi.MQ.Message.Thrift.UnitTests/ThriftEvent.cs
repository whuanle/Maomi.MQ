using Thrift.Protocol;
using Thrift.Protocol.Entities;

namespace Maomi.MQ.Message.Thrift.UnitTests;

public class ThriftEvent : TBase
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public async Task ReadAsync(TProtocol protocol, CancellationToken cancellationToken)
    {
        await protocol.ReadStructBeginAsync(cancellationToken);

        while (true)
        {
            var field = await protocol.ReadFieldBeginAsync(cancellationToken);
            if (field.Type == TType.Stop)
            {
                break;
            }

            switch (field.ID)
            {
                case 1 when field.Type == TType.I32:
                    Id = await protocol.ReadI32Async(cancellationToken);
                    break;
                case 2 when field.Type == TType.String:
                    Name = await protocol.ReadStringAsync(cancellationToken);
                    break;
                default:
                    await SkipFieldAsync(protocol, field.Type, cancellationToken);
                    break;
            }

            await protocol.ReadFieldEndAsync(cancellationToken);
        }

        await protocol.ReadStructEndAsync(cancellationToken);
    }

    public async Task WriteAsync(TProtocol protocol, CancellationToken cancellationToken)
    {
        await protocol.WriteStructBeginAsync(new TStruct(nameof(ThriftEvent)), cancellationToken);

        await protocol.WriteFieldBeginAsync(new TField(nameof(Id), TType.I32, 1), cancellationToken);
        await protocol.WriteI32Async(Id, cancellationToken);
        await protocol.WriteFieldEndAsync(cancellationToken);

        await protocol.WriteFieldBeginAsync(new TField(nameof(Name), TType.String, 2), cancellationToken);
        await protocol.WriteStringAsync(Name, cancellationToken);
        await protocol.WriteFieldEndAsync(cancellationToken);

        await protocol.WriteFieldStopAsync(cancellationToken);
        await protocol.WriteStructEndAsync(cancellationToken);
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
            _ => throw new NotSupportedException($"Field type '{fieldType}' is not supported by this test model."),
        };
    }
}
