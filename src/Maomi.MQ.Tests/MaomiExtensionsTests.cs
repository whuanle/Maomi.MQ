using Maomi.MQ;
using Maomi.MQ.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Reflection;
using Xunit;

public class MaomiExtensionsTests
{
    [Fact]
    public void AddMaomiMQ_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MqOptionsBuilder> mqOptionsBuilder = options =>
        {
            options.AppName = "TestApp";
            options.WorkId = 1;
            options.AutoQueueDeclare = true;
            options.Rabbit = factory => factory.HostName = "localhost";
        };
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        services.AddMaomiMQ(mqOptionsBuilder, assemblies);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mqOptions = serviceProvider.GetService<MqOptions>();
        Assert.NotNull(mqOptions);
        Assert.Equal("TestApp", mqOptions.AppName);
        Assert.Equal(1, mqOptions.WorkId);
        Assert.True(mqOptions.AutoQueueDeclare);
        Assert.NotNull(mqOptions.ConnectionFactory);
    }
}
