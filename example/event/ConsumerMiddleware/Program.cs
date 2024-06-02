using ConsumerMiddleware.Events;
using Maomi.MQ;
using Microsoft.EntityFrameworkCore;

namespace Web2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddLogging();

            using BloggingContext bloggingContext = new();
            // 如果数据库不存在，则会创建数据库及其所有表。
            bloggingContext.Database.EnsureCreated();
            bloggingContext.Database.Migrate();

            builder.Services.AddDbContext<BloggingContext>();

            builder.Services.AddMaomiMQ(options =>
            {
                options.WorkId = 1;
            }, options =>
            {
                options.HostName = "192.168.3.248";
            }, new System.Reflection.Assembly[] { typeof(Program).Assembly });


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
}
