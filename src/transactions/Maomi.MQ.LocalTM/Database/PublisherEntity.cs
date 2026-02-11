// <copyright file="PublisherEntity.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1600
#pragma warning disable CS8618
#pragma warning disable IDE1006
#pragma warning disable SA1300

namespace Maomi.MQ.Transaction.Database;

public class PublisherEntity
{
    public long message_id { get; set; }

    public string exchange { get; set; }

    public string routing_key { get; set; }

    public string properties { get; set; }

    public string message_body { get; set; }

    public string message_text { get; set; }

    public int status { get; set; }

    public DateTimeOffset create_time { get; set; }

    public DateTimeOffset update_time { get; set; }

    public int retry_count { get; set; }
}