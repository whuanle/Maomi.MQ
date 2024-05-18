// <copyright file="DefaultJsonSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Defaults;

/// <summary>
/// Default serializer.
/// </summary>
public class DefaultJsonSerializer : IJsonSerializer
{
    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
        where TObject : class
    {
        return System.Text.Json.JsonSerializer.Deserialize<TObject>(bytes);
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
        where TObject : class
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
    }
}
