// <copyright file="DefaultJsonMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Maomi.MQ.Defaults;

/// <summary>
/// Default json serializer.
/// </summary>
public class DefaultJsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = false,
            IgnoreReadOnlyProperties = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        return options;
    }

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        return System.Text.Json.JsonSerializer.Deserialize<TObject>(bytes, JsonSerializerOptions);
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj, JsonSerializerOptions);
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>(TObject obj)
    {
        return true;
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>()
    {
        return true;
    }
}
