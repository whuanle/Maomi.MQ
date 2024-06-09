using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{

    [HttpGet("get")]
    public string Get()
    {
        return "1";
    }
}

public class AAA()
{

}

[Consumer("aaa")]
public class MyConsumer : IConsumer<AAA>
{
    public Task ExecuteAsync(EventBody<AAA> message)
    {
        throw new NotImplementedException();
    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<AAA>? message)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FallbackAsync(EventBody<AAA>? message)
    {
        throw new NotImplementedException();
    }
}