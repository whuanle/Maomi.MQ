using AutoFixture.Xunit2;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Tests;

public class JsonSerializerTests
{
    [Theory, AutoData]
    public void Serializer(SerializerTestEvent model)
    {
        var services = MaomiHostHelper.BuildEmpty();

        var ioc = services.BuildServiceProvider();
        var serializer = ioc.GetRequiredService<IMessageSerializer>();

        var bytes = serializer.Serializer(model);
        var newModel = serializer.Deserialize<SerializerTestEvent>(bytes);

        Assert.NotNull(newModel);
        Assert.Equal(model.ValueA, newModel.ValueA);
        Assert.Equal(model.ValueB, newModel.ValueB);
        Assert.Equal(model.ValueC, newModel.ValueC);
        Assert.Equal(model.ValueD, newModel.ValueD);
        Assert.Equal(model.ValueE, newModel.ValueE);
        Assert.Equal(model.ValueF, newModel.ValueF);
        Assert.Equal(model.ValueG, newModel.ValueG);
        Assert.Equal(model.ValueH, newModel.ValueH);
        Assert.Equal(model.ValueI, newModel.ValueI);
        Assert.Equal(model.ValueJ, newModel.ValueJ);
        Assert.Equal(model.ValueK, newModel.ValueK);
        Assert.Equal(model.ValueL, newModel.ValueL);
    }

    public class SerializerTestEvent
    {
        public bool ValueA { get; set; }
        public sbyte ValueB { get; set; }
        public byte ValueC { get; set; }
        public short ValueD { get; set; }
        public ushort ValueE { get; set; }
        public int ValueF { get; set; }
        public uint ValueG { get; set; }
        public long ValueH { get; set; }
        public ulong ValueI { get; set; }
        public float ValueJ { get; set; }
        public double ValueK { get; set; }
        public decimal ValueL { get; set; }
        public char ValueM { get; set; }
    }
}
