// <copyright file="EventBusTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

public class RegisterQueue : Tuple<bool, IConsumerOptions>
{
    public bool IsRegister { get; init; }
    public IConsumerOptions Options { get; init; }
    public RegisterQueue(bool item1, IConsumerOptions item2)
        : base(item1, item2)
    {
        IsRegister = item1;
        Options = item2;
    }
}
