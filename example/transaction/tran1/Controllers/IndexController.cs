using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Transactions;
using tran1.DB;
using tran1.MQ;

namespace tran1.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly ILogger<IndexController> _logger;
    private readonly MyDbContext _myDbContext;
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(ILogger<IndexController> logger, MyDbContext myDbContext, IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _myDbContext = myDbContext;
        _messagePublisher = messagePublisher;
    }

    [HttpGet(template: "p1")]
    public async Task<string> Publish1()
    {
        var tran = await _myDbContext.Database.BeginTransactionAsync();

        using var dbConnection = _myDbContext.Database.GetDbConnection();
        var transcationPublisher = _messagePublisher.CreateDBTransaction(dbConnection, tran.GetDbTransaction());

        var testEntity = new TestEneity
        {
            Name = "aaa",
            CreatedAt = DateTimeOffset.Now
        };

        _myDbContext.Add(testEntity);
        await _myDbContext.SaveChangesAsync();

        await transcationPublisher.PublishAsync(string.Empty, "tran_p1", new CreateTestEntityMessage
        {
            Id = testEntity.Id,
            Name = "aaa",
            CreatedAt = DateTimeOffset.Now
        });

        await tran.CommitAsync();

        return "ok";
    }


    [HttpGet(template: "p2")]
    public async Task<string> Publish2()
    {
        var dbConnection = _myDbContext.Database.GetDbConnection();
        if (dbConnection.State != System.Data.ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        var transcationPublisher = _messagePublisher.CreateDBTransaction();
        using var tran = new TransactionScope(scopeOption: TransactionScopeOption.Required, transactionOptions: new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted
        }, asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var testEntity = new TestEneity
        {
            Name = "aaa",
            CreatedAt = DateTimeOffset.Now
        };

        _myDbContext.Add(testEntity);
        await _myDbContext.SaveChangesAsync();

        await transcationPublisher.PublishAsync(string.Empty, "tran_p1", new CreateTestEntityMessage
        {
            Id = testEntity.Id,
            Name = "aaa",
            CreatedAt = DateTimeOffset.Now
        });

        tran.Complete();

        return "ok";
    }
}
