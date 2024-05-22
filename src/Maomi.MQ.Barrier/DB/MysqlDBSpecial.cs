// <copyright file="DBSpecial.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Barrier.DB;

public class MysqlDBSpecial : DBSpecial
{
    public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
    {
        return string.Format("insert ignore into {0}", tableAndValues);
    }

    public string GetPlaceHoldSQL(string sql)
    {
        return sql;
    }

    public string GetXaSQL(string command, string xid)
    {
        if (command == "abort")
        {
            command = "rollback";
        }

        return string.Format("xa {0} {1}", command, xid);
    }
}