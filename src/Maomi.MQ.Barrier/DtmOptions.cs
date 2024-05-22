// <copyright file="Class1.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Barrier;

public enum DBType
{
    Mysql,
    Postgres
}

public class DtmOptions
{
    /// <summary>
    /// barrier table type. default mysql
    /// </summary>
    public DBType DBType { get; set; } = DBType.Mysql;

    /// <summary>
    /// barrier table name, default barrier
    /// </summary>
    public string BarrierTableName { get; set; } = "barrier";

    /// <summary>
    /// branch request timeout in milliseconds, default 10,000 milliseconds(10s)
    /// </summary>
    public int BranchTimeout { get; set; } = 10 * 1000;
}
