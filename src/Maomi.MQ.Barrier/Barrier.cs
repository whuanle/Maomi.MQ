// <copyright file="Class1.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Barrier.DB;
using System.Data;
using System.Data.Common;

namespace Maomi.MQ.Barrier;

/// <summary>
/// BarrierBusiFunc type for busi func.
/// </summary>
/// <param name="connection"></param>
/// <returns><see cref="Task"/>.</returns>
public delegate Task BarrierBusiFunc(DbTransaction connection);

public class BranchBarrierProvider : IBranchBarrierProvider
{
    private readonly DtmOptions _dtmOptions;

    public BranchBarrierProvider(DtmOptions dtmOptions)
    {
        _dtmOptions = dtmOptions;
    }

    public BranchBarrier CreateBranchBarrier(string transType, string gid, string branchID, string op)
    {
        var ti = new BranchBarrier(transType, gid, branchID, op, _dtmOptions);
        return ti;
    }

}

public partial class BranchBarrier
{
    private readonly DbBranchBarrier _dbBranchBarrier;
    public BranchBarrier(string transType, string gid, string branchID, string op, DtmOptions dtmOptions, DbBranchBarrier dbBranchBarrier)
    {
        TransType = transType;
        Gid = gid;
        BranchID = branchID;
        Op = op;
        DBType = dtmOptions.DBType.ToString().ToLower();
        BarrierTableName = dtmOptions.BarrierTableName;

        ArgumentNullException.ThrowIfNull(TransType);
        ArgumentNullException.ThrowIfNull(Gid);
        ArgumentNullException.ThrowIfNull(BranchID);
        ArgumentNullException.ThrowIfNull(Op);
        _dbBranchBarrier = dbBranchBarrier;
    }

    public string TransType { get; private set; }
    public string Gid { get; private set; }

    public string BranchID { get; private set; }

    public string Op { get; private set; }

    public int BarrierID { get; private set; }

    public string DBType { get; private set; }

    public string BarrierTableName { get; private set; }
}


public partial class BranchBarrier
{
    /// <summary>
    /// BranchBarrier every branch info.
    /// </summary>
    /// <returns>Branch info.</returns>
    public override string ToString()
    {
        return $"transInfo: {TransType} {Gid} {BranchID} {Op}";
    }

    public string NewBarrierID()
    {
        BarrierID++;
        return String.Format("{0:00}", BarrierID);
    }
}

public partial class BranchBarrier
{
    public async Task Call(DbTransaction tx,BarrierBusiFunc busiCall)
    {
        var bid = NewBarrierID();
      
        try
        {
            await tx.CommitAsync();
            var originOP = new Dictionary<string, string>()
            {
                { Dtmimp.OpCancel,Dtmimp.OpTry},
                { Dtmimp.OpCompensate,Dtmimp.OpAction},
                { Dtmimp.OpRollback,Dtmimp.OpAction}
            }[Op];

            var originAffected = await _dbBranchBarrier.InsertBarrier(tx, this.TransType, this.Gid, this.BranchID, this.Op, this.BarrierID, originOP, barrierTableName);
            var currentAffected =await _dbBranchBarrier.InsertBarrier(tx, this.TransType, this.Gid, this.BranchID, this.Op, this.BarrierID, originOP, barrierTableName);

            if(Op == Consts.MsgDoOp && currentAffected == 0)
            {
                throw new Exception("DUPLICATED");
            }
            if((Op == Consts.OpCancel||Op == Consts.OpCompensate || Op == Consts.OpRollback) && originAffected>0||currentAffected == 0)
            {
                return; 
            }

            await busiCall(tx);
        }
        finally
        {
            await tx.RollbackAsync();
        }
    }
}

/// <summary>
/// 数据库处理子事务屏障
/// </summary>
public class DbBranchBarrier
{
    private readonly DtmOptions _options;
    private readonly DBSpecial _special;

    public DbBranchBarrier(DtmOptions options, DBSpecial dbSpecial)
    {
        _options = options;
        _special = dbSpecial;
    }

    /// <summary>
    /// 数据库中插入子事务屏障
    /// </summary>
    /// <param name="db"></param>
    /// <param name="transType"></param>
    /// <param name="gid"></param>
    /// <param name="branchID"></param>
    /// <param name="op"></param>
    /// <param name="barrierID"></param>
    /// <param name="reason"></param>
    /// <param name="tx"></param>
    /// <returns></returns>
    public async Task<(int, Exception?)> InsertBarrier(DbConnection db, string transType, string gid, string branchID, string op, string barrierID, string reason, DbTransaction? tx = null)
    {
        if (db == null)
        {
            return (-1, null);
        }
        if (string.IsNullOrWhiteSpace(op))
        {
            return (0, null);
        }

        try
        {
            var str = string.Concat(_options.BarrierTableName, "(trans_type, gid, branch_id, op, barrier_id, reason) values(@trans_type,@gid,@branch_id,@op,@barrier_id,@reason)");
            var sql = _special.GetInsertIgnoreTemplate(str, "uniq_barrier");

            sql = _special.GetPlaceHoldSQL(sql);
            using var command = db.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new { trans_type = transType, gid, branch_id = branchID, op, barrier_id = barrierID, reason });
            command.Transaction = tx;

            var affected = await command.ExecuteNonQueryAsync();
            //var affected = await db.ExecuteAsync(
            //    sql,
            //    new { trans_type = transType, gid, branch_id = branchID, op, barrier_id = barrierID, reason },
            //    transaction: tx);

            return (affected, null);
        }
        catch (Exception ex)
        {
            return (0, ex);
        }
    }

    private static void AddParameters(IDbCommand command, object[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            var param = command.CreateParameter();
            param.ParameterName = $"@param{i + 1}";
            param.Value = values[i];
            command.Parameters.Add(param);
        }
    }
}

//public static class BranchBarrierFactory
//{
//    public static async Task<long> InsertBarrier(DbTransaction tx, string transType, string gid, string branchID, string op, string barrierID, string reason, string dbType = "", string barrierTableName = "")
//    {
//        if (string.IsNullOrEmpty(op))
//        {
//            return 0;
//        }

//        if (string.IsNullOrEmpty(dbType))
//        {
//            throw new Exception(dbType);
//        }

//        if (string.IsNullOrEmpty(barrierTableName))
//        {
//            barrierTableName = BarrierTableName;
//        }

//        var sql = DefaultDBSpecial.GetDBSpecial(dbType).GetInsertIgnoreTemplate($"{barrierTableName}(trans_type, gid, branch_id, op, barrier_id, reason) values(@transType, @gid, @branchID, @op, @barrierID, @reason)", "uniq_barrier");
//        return await DBExec(dbType, tx, sql, new { transType, gid, branchID, op, barrierID, reason });
//    }

//    public static async Task<(long affected, Exception error)> DBExec(string dbType, DbTransaction db, string sql, params object[] values)
//    {
//        if (string.IsNullOrEmpty(sql))
//        {
//            return (0, null);
//        }

//        var stopwatch = Stopwatch.StartNew();
//        sql = DefaultDBSpecial.GetDBSpecial(dbType).GetPlaceHoldSQL(sql);

//        try
//        {
//            var command = db.CreateCommand();
//            command.CommandText = sql;
//            AddParameters(command, values);

//            var affectedRows = await command.ExecuteNonQueryAsync();
//            stopwatch.Stop();

//            logger.Debug($"used: {stopwatch.ElapsedMilliseconds} ms affected: {affectedRows} for {sql} {values}");
//            return (affectedRows, null);
//        }
//        catch (Exception ex)
//        {
//            stopwatch.Stop();
//            logger.Error($"used: {stopwatch.ElapsedMilliseconds} ms exec error: {ex} for {sql} {values}");
//            return (0, ex);
//        }
//    }

//    private static void AddParameters(IDbCommand command, object[] values)
//    {
//        for (int i = 0; i < values.Length; i++)
//        {
//            var param = command.CreateParameter();
//            param.ParameterName = $"@param{i + 1}";
//            param.Value = values[i];
//            command.Parameters.Add(param);
//        }
//    }

//}