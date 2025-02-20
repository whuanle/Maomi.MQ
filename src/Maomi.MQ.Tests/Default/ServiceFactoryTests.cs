using Maomi.MQ.Default;
using Moq;

namespace Maomi.MQ.Tests;

public class ServiceFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IMessageSerializer> _serializerMock;
    private readonly Mock<IRetryPolicyFactory> _retryPolicyFactoryMock;
    private readonly Mock<IIdFactory> _idFactoryMock;
    private readonly MqOptions _options;

    public ServiceFactoryTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serializerMock = new Mock<IMessageSerializer>();
        _retryPolicyFactoryMock = new Mock<IRetryPolicyFactory>();
        _idFactoryMock = new Mock<IIdFactory>();
        _options = new MqOptions();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var serviceFactory = new ServiceFactory(
            _serviceProviderMock.Object,
            _options,
            _serializerMock.Object,
            _retryPolicyFactoryMock.Object,
            _idFactoryMock.Object);

        Assert.Equal(_serviceProviderMock.Object, serviceFactory.ServiceProvider);
        Assert.Equal(_options, serviceFactory.Options);
        Assert.Equal(_serializerMock.Object, serviceFactory.Serializer);
        Assert.Equal(_retryPolicyFactoryMock.Object, serviceFactory.RetryPolicyFactory);
        Assert.Equal(_idFactoryMock.Object, serviceFactory.Ids);
    }
}
