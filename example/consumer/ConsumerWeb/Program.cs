using Maomi.MQ;
using Polly;
using Polly.Retry;

namespace ActivitySourceApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole().AddDebug();

        builder.Services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
        }, options =>
        {
            options.HostName = "192.168.1.4";
            options.ClientProvidedName = "aaa";
        }, new System.Reflection.Assembly[] { typeof(Program).Assembly });

        builder.Services.AddSingleton<IRetryPolicyFactory, MyDefaultRetryPolicyFactory>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
public class MyDefaultRetryPolicyFactory : IRetryPolicyFactory
{
    private readonly ILogger<MyDefaultRetryPolicyFactory> _logger;

    public MyDefaultRetryPolicyFactory(ILogger<MyDefaultRetryPolicyFactory> logger)
    {
        _logger = logger;
    }

    public Task<AsyncRetryPolicy> CreatePolicy(string queue)
    {
        // Create a retry policy.
        // 创建重试策略.
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(500),
                onRetry: async (Exception exception, TimeSpan timeSpan, int retryCount, Context context) =>
                {
                    _logger.LogDebug("Retry execution event,queue [{Queue}],retry count [{RetryCount}],timespan [{TimeSpan}]", queue, retryCount, timeSpan);
                });

        return Task.FromResult(retryPolicy);
    }

    public Task<AsyncRetryPolicy> CreatePolicy(string queue, long id)
    {
        throw new NotImplementedException();
    }
}