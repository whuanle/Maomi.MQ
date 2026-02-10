using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using ProtoDemo.Proto;
using Xunit;

namespace Maomi.MQ.Message.Protobuf.Tests;

public class MessageSerializerFactoryTests
{
    private readonly MessageSerializerFactory _factory;

    public MessageSerializerFactoryTests()
    {
        _factory = new MessageSerializerFactory(MockSerializer, MockDeserializer);
    }

    private IMessageSerializer MockSerializer(Type type)
    {
        if (type.IsAssignableTo(typeof(Google.Protobuf.IMessage)))
        {
            return new ProtobufMessageSerializer();
        }
        else
        {
            return new DefaultJsonMessageSerializer();
        }
    }

    private IMessageSerializer MockDeserializer(Type type, MessageHeader messageHeader)
    {
        if (type.IsAssignableTo(typeof(Google.Protobuf.IMessage)) || messageHeader.ContentType == "application/x-protobuf")
        {
            return new ProtobufMessageSerializer();
        }
        else
        {
            return new DefaultJsonMessageSerializer();
        }
    }

    [Fact]
    public void TestContentEncoding()
    {
        var m1 = _factory.GetMessageSerializer(typeof(TestEvent));
        var m2 = _factory.GetMessageSerializer(typeof(Person));
        Assert.Equal("UTF-8", m1.ContentEncoding);
        Assert.Equal("UTF-8", m2.ContentEncoding);
    }

    [Fact]
    public void TestContentType()
    {
        var m1 = _factory.GetMessageSerializer(typeof(TestEvent));
        var m2 = _factory.GetMessageSerializer(typeof(Person));

        Assert.Equal("application/json", m1.ContentType);
        Assert.Equal("application/x-protobuf", m2.ContentType);
    }

    [Fact]
    public void TestDeserialize()
    {
        var testEvent = new TestEvent { Id = 1, Message = "Test" };
        var person = new Person { Id = 1, Name = "Test" };

        var r1 = _factory.Serializer(testEvent);
        var r2 = _factory.Serializer(person);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
    }

    [Fact]
    public void TestSerializer()
    {
        var testEvent = new TestEvent { Id = 1, Message = "Test" };
        var person = new Person { Id = 1, Name = "Test" };

        var r1 = _factory.Serializer(testEvent);
        var r2 = _factory.Serializer(person);

        var d1 = _factory.Deserialize<TestEvent>(r1);
        var d2 = _factory.Deserialize<Person>(r2);

        Assert.NotNull(d1);
        Assert.NotNull(d2);

        Assert.Equal(testEvent.Id, d1.Id);
        Assert.Equal(testEvent.Message, d1.Message);

        Assert.Equal(person.Id, d2.Id);
        Assert.Equal(person.Name, d2.Name);
    }
}
