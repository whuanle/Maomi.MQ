// <copyright file="Class1.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Barrier.Dtmimp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Maomi.MQ.Barrier;


public partial class BranchBarrier
{
    public string TransType { get; set; }
    public string Gid { get; set; }

    public string BranchID { get; set; }

    public string Op { get; set; }

    public int BarrierID { get; set; }

    public string DBType { get; set; }

    public string BarrierTableName { get; set; }
}

/// <summary>
/// BarrierBusiFunc type for busi func.
/// </summary>
/// <param name="connection"></param>
/// <returns><see cref="Task"/>.</returns>
public delegate Task BarrierBusiFunc(DbTransaction connection);

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

    /// <summary>
    /// BarrierFromQuery construct transaction info from request.
    /// </summary>
    /// <param name="transType"></param>
    /// <param name="gid"></param>
    /// <param name="branchId"></param>
    /// <param name="op"></param>
    /// <returns><see cref="BranchBarrier"/>.</returns>
    public static BranchBarrier BarrierFromQuery(string transType, string gid, string branchId, string op)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(transType);
        ArgumentNullException.ThrowIfNullOrEmpty(gid);
        ArgumentNullException.ThrowIfNullOrEmpty(branchId);
        ArgumentNullException.ThrowIfNullOrEmpty(op);

        var ti = new BranchBarrier()
        {
            TransType = transType,
            Gid = gid,
            BranchID = branchId,
            Op = op
        };
        return ti;
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
                { Consts.OpCancel,Consts.OpTry},
                { Consts.OpCompensate,Consts.OpAction},
                { Consts.OpRollback,Consts.OpAction}
            }[Op];

            var originAffected = await BranchBarrierFactory.InsertBarrier(tx, this.TransType, this.Gid, this.BranchID, this.Op, this.BarrierID, originOP, barrierTableName);
            var currentAffected =await  BranchBarrierFactory.InsertBarrier(tx, this.TransType, this.Gid, this.BranchID, this.Op, this.BarrierID, originOP, barrierTableName);

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

public static class BranchBarrierFactory
{
    public static async Task<long> InsertBarrier(DbTransaction tx, string transType, string gid, string branchID, string op, string barrierID, string reason, string dbType = "", string barrierTableName = "")
    {
        if (string.IsNullOrEmpty(op))
        {
            return 0;
        }

        if (string.IsNullOrEmpty(dbType))
        {
            throw new Exception(dbType);
        }

        if (string.IsNullOrEmpty(barrierTableName))
        {
            barrierTableName = BarrierTableName;
        }

        var sql = DefaultDBSpecial.GetDBSpecial(dbType).GetInsertIgnoreTemplate($"{barrierTableName}(trans_type, gid, branch_id, op, barrier_id, reason) values(@transType, @gid, @branchID, @op, @barrierID, @reason)", "uniq_barrier");
        return await DBExec(dbType, tx, sql, new { transType, gid, branchID, op, barrierID, reason });
    }

    public static async Task<(long affected, Exception error)> DBExec(string dbType, DbTransaction db, string sql, params object[] values)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return (0, null);
        }

        var stopwatch = Stopwatch.StartNew();
        sql = DefaultDBSpecial.GetDBSpecial(dbType).GetPlaceHoldSQL(sql);

        try
        {
            var command = db.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, values);

            var affectedRows = await command.ExecuteNonQueryAsync();
            stopwatch.Stop();

            logger.Debug($"used: {stopwatch.ElapsedMilliseconds} ms affected: {affectedRows} for {sql} {values}");
            return (affectedRows, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.Error($"used: {stopwatch.ElapsedMilliseconds} ms exec error: {ex} for {sql} {values}");
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