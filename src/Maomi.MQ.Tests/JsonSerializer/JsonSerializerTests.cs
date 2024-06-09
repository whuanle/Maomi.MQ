using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Tests.JsonSerializer;

public class JsonSerializerTests
{
    [Fact]
    public void Serializer()
    {
        var services = MaomiHostHelper.BuildEmpty();

        var ioc = services.BuildServiceProvider();
        var serializer = ioc.GetRequiredService<IJsonSerializer>();

        var model = new TestEvent
        {
            Id = 1,
            Message = "test"
        };

        var bytes = serializer.Serializer(model);
        var newModel = serializer.Deserialize<TestEvent>(bytes);

        Assert.NotNull(newModel);
        Assert.Equal(model.Id, newModel.Id);
        Assert.Equal(model.Message, newModel.Message);  
    }

    private class TestEvent
    {
        public int Id { get; set; }
        public string Message { get; set; } = null!;
    }
}
