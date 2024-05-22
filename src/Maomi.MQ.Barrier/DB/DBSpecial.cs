// <copyright file="DBSpecial.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Barrier.DB;

/// <summary>
/// DBSpecial.数据库服务.
/// </summary>
public interface DBSpecial
{
    string GetPlaceHoldSQL(string sql);
    string GetInsertIgnoreTemplate(string tableAndValues, string pgConstraint);
    string GetXaSQL(string command, string xid);
}