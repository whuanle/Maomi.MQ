// <copyright file="DefaultIdFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Yitter.IdGenerator;

namespace Maomi.MQ.Default;

/// <summary>
/// Snowflake id generator.<br />
/// 雪花 id 生成器.
/// </summary>
public class DefaultIdFactory : IIdFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultIdFactory"/> class.
    /// </summary>
    /// <param name="workId"></param>
    public DefaultIdFactory(ushort workId)
    {
        var options = new IdGeneratorOptions(workId) { SeqBitLength = 10 };
        YitIdHelper.SetIdGenerator(options);
    }

    /// <inheritdoc />
    public long NextId() => YitIdHelper.NextId();
}
