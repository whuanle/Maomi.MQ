using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Maomi.MQ.RedisRetry
{
    public static class RedisRetryExtensions
    {
        public static IServiceCollection AddMaomiMQRedisRetry(this IServiceCollection services, Func<IServiceProvider, IDatabase> func)
        {

            services.AddTransient<IPolicyFactory, RedisRetryPolicyFactory>(s=>
            {
                var logger = s.GetRequiredService<ILogger<DefaultPolicyFactory>>();
                var redis = func.Invoke(s);
                var mqOptions = s.GetRequiredService<MqOptions>();
                return new RedisRetryPolicyFactory(logger, redis, mqOptions);
            });
            return services;
        }
    }
}
