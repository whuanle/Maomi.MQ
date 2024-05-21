using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Barrier;

/// <summary>
/// 事务类型 <see cref="TranTypes"/>
/// </summary>
public enum TranSchema
{
    /// <summary>
    /// MSG
    /// </summary>
    MSG = 0,

    /// <summary>
    /// SAGA
    /// </summary>
    SAGA = 1,

    /// <summary>
    /// TCC
    /// </summary>
    TCC = 2,

    /// <summary>
    /// XA
    /// </summary>
    XA = 3
}

/// <summary>
/// 事务类型
/// </summary>
public static class TranTypes
{
    /// <summary>
    /// MSG
    /// </summary>
    public const string MSG = "msg";

    /// <summary>
    /// TCC
    /// </summary>
    public const string TCC = "tcc";

    /// <summary>
    /// XA
    /// </summary>
    public const string XA = "xa";

    /// <summary>
    /// SAGA
    /// </summary>
    public const string SAGA = "saga";


    /// <summary>
    /// 正向操作
    /// </summary>
    public const string Action = "action";

    /// <summary>
    /// 回滚操作
    /// </summary>
    public const string Compensate = "compensate";

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <param name="transSchema"></param>
    /// <returns></returns>
    public static string ConvertString(this TranSchema transSchema)
    {
        switch (transSchema)
        {
            case TranSchema.MSG: return MSG;
            case TranSchema.TCC: return TCC;
            case TranSchema.SAGA: return SAGA;
            case TranSchema.XA: return XA;
        }
        return MSG;
    }
}