using Maomi.MQ.Barrier.DB;

namespace Maomi.MQ.Barrier;
public partial class Dtmimp
{
    public const string Mysql = "Mysql";

    public const string Postgres = "Postgres";

    /// <summary>
    /// ResultFailure for result of a trans/trans branch,Same as HTTP status 409 and GRPC code 10.
    /// </summary>
    public const string ResultFailure = "FAILURE";

    /// <summary>
    /// ResultSuccess for result of a trans/trans branch,Same as HTTP status 200 and GRPC code 0.
    /// </summary>
    public const string ResultSuccess = "SUCCESS";

    /// <summary>
    // ResultOngoing for result of a trans/trans branch,
    // Same as HTTP status 425 and GRPC code 9.
    /// </summary>
    public const string ResultOngoing = "ONGOING";

    /// <summary>
    // OpTry branch type for TCC.
    /// </summary>
    public const string OpTry = "try";

    /// <summary>
    // OpConfirm branch type for TCC.
    /// </summary>
    public const string OpConfirm = "confirm";

    /// <summary>
    // OpCancel branch type for TCC.
    /// </summary>
    public const string OpCancel = "cancel";

    /// <summary>
    // OpAction branch type for message, SAGA, XA.
    /// </summary>
    public const string OpAction = "action";

    /// <summary>
    // OpCompensate branch type for SAGA.
    /// </summary>
    public const string OpCompensate = "compensate";

    /// <summary>
    // OpCommit branch type for XA.
    /// </summary>
    public const string OpCommit = "commit";

    /// <summary>
    // OpRollback branch type for XA
    /// </summary>
    public const string OpRollback = "rollback";

    /// <summary>
    // DBTypeMysql const for driver mysql
    public const string DBTypeMysql = "mysql";

    /// <summary>
    // DBTypePostgres const for driver postgres
    public const string DBTypePostgres = "postgres";

    /// <summary>
    // DBTypeRedis const for driver redis
    public const string DBTypeRedis = "redis";

    /// <summary>
    // Jrpc const for json-rpc
    public const string Jrpc = "json-rpc";

    /// <summary>
    // JrpcCodeFailure const for json-rpc failure
    public const int JrpcCodeFailure = -32901;

    /// <summary>
    // JrpcCodeOngoing const for json-rpc ongoing
    public const int JrpcCodeOngoing = -32902;

    /// <summary>
    // MsgDoBranch0 const for DoAndSubmit barrier branch
    public const string MsgDoBranch0 = "00";

    /// <summary>
    // MsgDoBarrier1 const for DoAndSubmit barrier barrierID
    public const string MsgDoBarrier1 = "01";

    /// <summary>
    // MsgDoOp const for DoAndSubmit barrier op
    public const string MsgDoOp = "msg";

    /// <summary>
    //MsgTopicPrefix const for Add topic msg
    public const string MsgTopicPrefix = "topic://";

    /// <summary>
    // XaBarrier1 const for xa barrier id
    public const string XaBarrier1 = "01";

    /// <summary>
    // ProtocolGRPC const for protocol grpc
    public const string ProtocolGRPC = "grpc";

    /// <summary>
    // ProtocolHTTP const for protocol http
    public const string ProtocolHTTP = "http";

    public const string DBTYPE_MYSQL = "mysql";
    public const string DBTYPE_POSTGRES = "postgres";
    public const string DBTYPE_SQLSERVER = "sqlserver";
    public const string PG_CONSTRAINT = "uniq_barrier";
    public const string MSG_BARRIER_REASON = "rollback";
    public const string MSG_BRANCHID = "00";
    public const string MSG_BARRIER_ID = "01";

    private static readonly Dictionary<string, DBSpecial> DbSpecials = new()
    {
        { DBTypeMysql, new MysqlDBSpecial() },
        { DBTypePostgres, new PostgresDBSpecial() }
    };

    public static DBSpecial GetDBSpecial(DBType dbType)
    {
        return DbSpecials[dbType.ToString().ToLower()];
    }
}
