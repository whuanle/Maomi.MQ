// <copyright file="IRoutingBindFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

public class RoutingProvider : IRoutingProvider
{
    public IConsumerOptions Get(IConsumerOptions consumerOptions)
    {
        return consumerOptions.Clone();
    }
}
