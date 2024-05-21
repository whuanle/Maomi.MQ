// <copyright file="DBSpecial.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Data;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Maomi.MQ.Barrier.Dtmimp;

namespace Maomi.MQ.Barrier;

public interface DBSpecial
{
    string GetPlaceHoldSQL(string sql);
    string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint);
    string GetXaSQL(string command, string xid);
}


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

public class PostgresDBSpecial : DBSpecial
{
    public string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint)
    {
        return string.Format("insert into {0} on conflict ON CONSTRAINT {1} do nothing", tableAndValues, pgConstraint);
    }

    public string GetPlaceHoldSQL(string sql)
    {
        var pos = 1;
        var parts = new List<string>();
        var b = 0;
        for (var i = 0; i < sql.Length; i++)
        {
            if (sql[i] == '?')
            {
                parts.Add(sql[b..i]);
                b = i + 1;
                parts.Add(string.Format("${0}", pos));
                pos++;
            }
        }

        parts.Add(sql[b..]);
        return string.Join(string.Empty, parts);
    }

    public string GetXaSQL(string command, string xid)
    {
        switch (command)
        {
            case "end": return "";
            case "start": return "begin";
            case "abort": return "rollback";
            case "prepare": return string.Format("prepare transaction '{0}'", xid);
            case "commit": return string.Format("commit prepared '{0}'", xid);
            case "rollback": return string.Format("rollback prepared '{0}'", xid);
        }

        throw new ArgumentException($"This command is of unknown type [{command}].");
    }
}