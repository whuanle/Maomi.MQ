extern alias gpb;
extern alias pbn;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Maomi.MQ.Defaults;
using Maomi.MQ.Serialization.Benchmarks.Models;
using Maomi.MQ.Serialization.Benchmarks.Proto;

namespace Maomi.MQ.Serialization.Benchmarks;

using GoogleProtobufSerializer = gpb::Maomi.MQ.ProtobufMessageSerializer;
using ProtobufNetSerializer = pbn::Maomi.MQ.ProtobufMessageSerializer;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class SerializerBenchmarks
{
    private readonly DefaultJsonMessageSerializer _defaultJsonSerializer = new();
    private readonly Maomi.MQ.MessagePackSerializer _messagePackSerializer = new();
    private readonly GoogleProtobufSerializer _googleProtobufSerializer = new();
    private readonly ProtobufNetSerializer _protobufNetSerializer = new();
    private readonly Maomi.MQ.ThriftMessageSerializer _thriftSerializer = new();

    private JsonMessage _jsonMessage = null!;
    private MessagePackMessage _messagePackMessage = null!;
    private Person _protobufMessage = null!;
    private ProtobufNetMessage _protobufNetMessage = null!;
    private ThriftMessage _thriftMessage = null!;

    private LargeMessage100 _largeMessage = null!;
    private PersonLarge100 _protobufLargeMessage = null!;

    private byte[] _jsonBytes = null!;
    private byte[] _messagePackBytes = null!;
    private byte[] _protobufBytes = null!;
    private byte[] _protobufNetBytes = null!;
    private byte[] _thriftBytes = null!;

    private byte[] _jsonLargeBytes = null!;
    private byte[] _messagePackLargeBytes = null!;
    private byte[] _protobufLargeBytes = null!;
    private byte[] _protobufNetLargeBytes = null!;
    private byte[] _thriftLargeBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _jsonMessage = new JsonMessage
        {
            Id = 1,
            Name = "benchmark",
            Email = "benchmark@maomi.mq",
        };

        _messagePackMessage = new MessagePackMessage
        {
            Id = 1,
            Name = "benchmark",
            Email = "benchmark@maomi.mq",
        };

        _protobufMessage = new Person
        {
            Id = 1,
            Name = "benchmark",
            Email = "benchmark@maomi.mq",
        };

        _protobufNetMessage = new ProtobufNetMessage
        {
            Id = 1,
            Name = "benchmark",
            Email = "benchmark@maomi.mq",
        };

        _thriftMessage = new ThriftMessage
        {
            Id = 1,
            Name = "benchmark",
            Email = "benchmark@maomi.mq",
        };

        _largeMessage = LargeMessage100.CreateSample();
        _protobufLargeMessage = ProtobufLargeMessageFactory.CreateSample();

        _jsonBytes = _defaultJsonSerializer.Serializer(_jsonMessage);
        _messagePackBytes = _messagePackSerializer.Serializer(_messagePackMessage);
        _protobufBytes = _googleProtobufSerializer.Serializer(_protobufMessage);
        _protobufNetBytes = _protobufNetSerializer.Serializer(_protobufNetMessage);
        _thriftBytes = _thriftSerializer.Serializer(_thriftMessage);

        _jsonLargeBytes = _defaultJsonSerializer.Serializer(_largeMessage);
        _messagePackLargeBytes = _messagePackSerializer.Serializer(_largeMessage);
        _protobufLargeBytes = _googleProtobufSerializer.Serializer(_protobufLargeMessage);
        _protobufNetLargeBytes = _protobufNetSerializer.Serializer(_largeMessage);
        _thriftLargeBytes = _thriftSerializer.Serializer(_largeMessage);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize", "DefaultJson", "Small")]
    public byte[] DefaultJson_Serialize() => _defaultJsonSerializer.Serializer(_jsonMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "DefaultJson", "Small")]
    public JsonMessage? DefaultJson_Deserialize() => _defaultJsonSerializer.Deserialize<JsonMessage>(_jsonBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "MessagePack", "Small")]
    public byte[] MessagePack_Serialize() => _messagePackSerializer.Serializer(_messagePackMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "MessagePack", "Small")]
    public MessagePackMessage? MessagePack_Deserialize() => _messagePackSerializer.Deserialize<MessagePackMessage>(_messagePackBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "GoogleProtobuf", "Small")]
    public byte[] Protobuf_Serialize() => _googleProtobufSerializer.Serializer(_protobufMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "GoogleProtobuf", "Small")]
    public Person? Protobuf_Deserialize() => _googleProtobufSerializer.Deserialize<Person>(_protobufBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "ProtobufNet", "Small")]
    public byte[] ProtobufNet_Serialize() => _protobufNetSerializer.Serializer(_protobufNetMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "ProtobufNet", "Small")]
    public ProtobufNetMessage? ProtobufNet_Deserialize() => _protobufNetSerializer.Deserialize<ProtobufNetMessage>(_protobufNetBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "Thrift", "Small")]
    public byte[] Thrift_Serialize() => _thriftSerializer.Serializer(_thriftMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "Thrift", "Small")]
    public ThriftMessage? Thrift_Deserialize() => _thriftSerializer.Deserialize<ThriftMessage>(_thriftBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "DefaultJson", "Large100")]
    public byte[] DefaultJson_Large100_Serialize() => _defaultJsonSerializer.Serializer(_largeMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "DefaultJson", "Large100")]
    public LargeMessage100? DefaultJson_Large100_Deserialize() => _defaultJsonSerializer.Deserialize<LargeMessage100>(_jsonLargeBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "MessagePack", "Large100")]
    public byte[] MessagePack_Large100_Serialize() => _messagePackSerializer.Serializer(_largeMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "MessagePack", "Large100")]
    public LargeMessage100? MessagePack_Large100_Deserialize() => _messagePackSerializer.Deserialize<LargeMessage100>(_messagePackLargeBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "GoogleProtobuf", "Large100")]
    public byte[] Protobuf_Large100_Serialize() => _googleProtobufSerializer.Serializer(_protobufLargeMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "GoogleProtobuf", "Large100")]
    public PersonLarge100? Protobuf_Large100_Deserialize() => _googleProtobufSerializer.Deserialize<PersonLarge100>(_protobufLargeBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "ProtobufNet", "Large100")]
    public byte[] ProtobufNet_Large100_Serialize() => _protobufNetSerializer.Serializer(_largeMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "ProtobufNet", "Large100")]
    public LargeMessage100? ProtobufNet_Large100_Deserialize() => _protobufNetSerializer.Deserialize<LargeMessage100>(_protobufNetLargeBytes);

    [Benchmark]
    [BenchmarkCategory("Serialize", "Thrift", "Large100")]
    public byte[] Thrift_Large100_Serialize() => _thriftSerializer.Serializer(_largeMessage);

    [Benchmark]
    [BenchmarkCategory("Deserialize", "Thrift", "Large100")]
    public LargeMessage100? Thrift_Large100_Deserialize() => _thriftSerializer.Deserialize<LargeMessage100>(_thriftLargeBytes);
}
