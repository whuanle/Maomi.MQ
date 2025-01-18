// <copyright file="ISingleChannelPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1401
#pragma warning disable SA1600

namespace Maomi.MQ;

/// <summary>
/// The publisher of an exclusive channel.<br />
/// 独占通道的发布者.
/// </summary>
public interface ISingleChannelPublisher : IMessagePublisher, IDisposable
{
}
