using System.Linq.Expressions;
using System.Reflection;
using Thrift.Protocol;
using Thrift.Protocol.Entities;

namespace Maomi.MQ.Serialization.Benchmarks.Models;

[global::MessagePack.MessagePackObject(true)]
[ProtoBuf.ProtoContract(ImplicitFields = ProtoBuf.ImplicitFields.AllPublic)]
public sealed class LargeMessage100 : TBase
{
    public const int FieldCount = 100;

    public int F001 { get; set; }

    public int F002 { get; set; }

    public int F003 { get; set; }

    public int F004 { get; set; }

    public int F005 { get; set; }

    public int F006 { get; set; }

    public int F007 { get; set; }

    public int F008 { get; set; }

    public int F009 { get; set; }

    public int F010 { get; set; }

    public int F011 { get; set; }

    public int F012 { get; set; }

    public int F013 { get; set; }

    public int F014 { get; set; }

    public int F015 { get; set; }

    public int F016 { get; set; }

    public int F017 { get; set; }

    public int F018 { get; set; }

    public int F019 { get; set; }

    public int F020 { get; set; }

    public int F021 { get; set; }

    public int F022 { get; set; }

    public int F023 { get; set; }

    public int F024 { get; set; }

    public int F025 { get; set; }

    public int F026 { get; set; }

    public int F027 { get; set; }

    public int F028 { get; set; }

    public int F029 { get; set; }

    public int F030 { get; set; }

    public int F031 { get; set; }

    public int F032 { get; set; }

    public int F033 { get; set; }

    public int F034 { get; set; }

    public int F035 { get; set; }

    public int F036 { get; set; }

    public int F037 { get; set; }

    public int F038 { get; set; }

    public int F039 { get; set; }

    public int F040 { get; set; }

    public int F041 { get; set; }

    public int F042 { get; set; }

    public int F043 { get; set; }

    public int F044 { get; set; }

    public int F045 { get; set; }

    public int F046 { get; set; }

    public int F047 { get; set; }

    public int F048 { get; set; }

    public int F049 { get; set; }

    public int F050 { get; set; }

    public int F051 { get; set; }

    public int F052 { get; set; }

    public int F053 { get; set; }

    public int F054 { get; set; }

    public int F055 { get; set; }

    public int F056 { get; set; }

    public int F057 { get; set; }

    public int F058 { get; set; }

    public int F059 { get; set; }

    public int F060 { get; set; }

    public int F061 { get; set; }

    public int F062 { get; set; }

    public int F063 { get; set; }

    public int F064 { get; set; }

    public int F065 { get; set; }

    public int F066 { get; set; }

    public int F067 { get; set; }

    public int F068 { get; set; }

    public int F069 { get; set; }

    public int F070 { get; set; }

    public int F071 { get; set; }

    public int F072 { get; set; }

    public int F073 { get; set; }

    public int F074 { get; set; }

    public int F075 { get; set; }

    public int F076 { get; set; }

    public int F077 { get; set; }

    public int F078 { get; set; }

    public int F079 { get; set; }

    public int F080 { get; set; }

    public int F081 { get; set; }

    public int F082 { get; set; }

    public int F083 { get; set; }

    public int F084 { get; set; }

    public int F085 { get; set; }

    public int F086 { get; set; }

    public int F087 { get; set; }

    public int F088 { get; set; }

    public int F089 { get; set; }

    public int F090 { get; set; }

    public int F091 { get; set; }

    public int F092 { get; set; }

    public int F093 { get; set; }

    public int F094 { get; set; }

    public int F095 { get; set; }

    public int F096 { get; set; }

    public int F097 { get; set; }

    public int F098 { get; set; }

    public int F099 { get; set; }

    public int F100 { get; set; }

    private static readonly Func<LargeMessage100, int>[] Getters = CreateGetters();
    private static readonly Action<LargeMessage100, int>[] Setters = CreateSetters();
    private static readonly TField[] Fields = CreateFields();

    public static LargeMessage100 CreateSample()
    {
        var message = new LargeMessage100();
        for (var index = 1; index <= FieldCount; index++)
        {
            Setters[index](message, index);
        }

        return message;
    }

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

            if (field.Type == TType.I32 && field.ID >= 1 && field.ID <= FieldCount)
            {
                Setters[field.ID](this, await protocol.ReadI32Async(cancellationToken).ConfigureAwait(false));
            }
            else
            {
                await SkipFieldAsync(protocol, field.Type, cancellationToken).ConfigureAwait(false);
            }

            await protocol.ReadFieldEndAsync(cancellationToken).ConfigureAwait(false);
        }

        await protocol.ReadStructEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(TProtocol protocol, CancellationToken cancellationToken)
    {
        await protocol.WriteStructBeginAsync(new TStruct(nameof(LargeMessage100)), cancellationToken).ConfigureAwait(false);

        for (short index = 1; index <= FieldCount; index++)
        {
            await protocol.WriteFieldBeginAsync(Fields[index], cancellationToken).ConfigureAwait(false);
            await protocol.WriteI32Async(Getters[index](this), cancellationToken).ConfigureAwait(false);
            await protocol.WriteFieldEndAsync(cancellationToken).ConfigureAwait(false);
        }

        await protocol.WriteFieldStopAsync(cancellationToken).ConfigureAwait(false);
        await protocol.WriteStructEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Func<LargeMessage100, int>[] CreateGetters()
    {
        var getters = new Func<LargeMessage100, int>[FieldCount + 1];
        for (var index = 1; index <= FieldCount; index++)
        {
            var propertyName = $"F{index:D3}";
            var property = typeof(LargeMessage100).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
            var messageParameter = Expression.Parameter(typeof(LargeMessage100), "message");
            var propertyAccess = Expression.Property(messageParameter, property);
            getters[index] = Expression.Lambda<Func<LargeMessage100, int>>(propertyAccess, messageParameter).Compile();
        }

        return getters;
    }

    private static Action<LargeMessage100, int>[] CreateSetters()
    {
        var setters = new Action<LargeMessage100, int>[FieldCount + 1];
        for (var index = 1; index <= FieldCount; index++)
        {
            var propertyName = $"F{index:D3}";
            var property = typeof(LargeMessage100).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
            var messageParameter = Expression.Parameter(typeof(LargeMessage100), "message");
            var valueParameter = Expression.Parameter(typeof(int), "value");
            var assignment = Expression.Assign(Expression.Property(messageParameter, property), valueParameter);
            setters[index] = Expression.Lambda<Action<LargeMessage100, int>>(assignment, messageParameter, valueParameter).Compile();
        }

        return setters;
    }

    private static TField[] CreateFields()
    {
        var fields = new TField[FieldCount + 1];
        for (short index = 1; index <= FieldCount; index++)
        {
            fields[index] = new TField($"F{index:D3}", TType.I32, index);
        }

        return fields;
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
