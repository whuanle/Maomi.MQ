using Maomi.MQ.Defaults;

namespace Maomi.MQ.UnitTests.Default;

public class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void ContentType_ShouldBeApplicationJson()
    {
        var serializer = new DefaultJsonMessageSerializer();
        Assert.Equal("application/json", serializer.ContentType);
    }

    [Fact]
    public void SerializeDeserialize_ShouldRoundtrip()
    {
        var serializer = new DefaultJsonMessageSerializer();
        var source = new DemoDto
        {
            Name = "alpha",
            Count = 42,
            Enabled = true,
        };

        var bytes = serializer.Serializer(source);
        var target = serializer.Deserialize<DemoDto>(bytes);

        Assert.NotNull(target);
        Assert.Equal(source.Name, target!.Name);
        Assert.Equal(source.Count, target.Count);
        Assert.Equal(source.Enabled, target.Enabled);
    }

    [Fact]
    public void Deserialize_InvalidPayload_ShouldThrow()
    {
        var serializer = new DefaultJsonMessageSerializer();
        var bytes = System.Text.Encoding.UTF8.GetBytes("{\"name\":");

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => serializer.Deserialize<DemoDto>(bytes));
    }

    [Fact]
    public void SerializerVerify_ShouldAlwaysReturnTrue()
    {
        var serializer = new DefaultJsonMessageSerializer();

        Assert.True(serializer.SerializerVerify(new DemoDto()));
        Assert.True(serializer.SerializerVerify<DemoDto>());
    }

    private sealed class DemoDto
    {
        public string? Name { get; set; }

        public int Count { get; set; }

        public bool Enabled { get; set; }
    }
}
