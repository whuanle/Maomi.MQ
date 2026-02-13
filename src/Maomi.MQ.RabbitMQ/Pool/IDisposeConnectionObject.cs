// <copyright file="IDisposeConnectionObject.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Pool;

/// <summary>
/// IConnection,IChannel pool.<br />
/// TCP 连接和通道.
/// </summary>
public interface IDisposeConnectionObject : IConnectionObject, IDisposable
{
}