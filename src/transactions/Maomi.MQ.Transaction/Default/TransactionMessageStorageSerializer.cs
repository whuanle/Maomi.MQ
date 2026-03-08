// <copyright file="TransactionMessageStorageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Maomi.MQ.Transaction.Default;

internal static class TransactionMessageStorageSerializer
{
    public static string SerializeHeader(MessageHeader messageHeader, JsonSerializerOptions options)
    {
        var payload = new StoredMessageHeader
        {
            Id = messageHeader.Id ?? string.Empty,
            Timestamp = messageHeader.Timestamp,
            AppId = messageHeader.AppId ?? string.Empty,
            ContentType = messageHeader.ContentType ?? string.Empty,
            Type = messageHeader.Type ?? string.Empty,
            Exchange = messageHeader.Exchange ?? string.Empty,
            RoutingKey = messageHeader.RoutingKey ?? string.Empty,
            Properties = CreateStoredBasicProperties(messageHeader.Properties as IReadOnlyBasicProperties),
        };

        return JsonSerializer.Serialize(payload, options);
    }

    public static MessageHeader DeserializeHeader(string messageHeaderJson, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(messageHeaderJson))
        {
            return new MessageHeader();
        }

        var payload = JsonSerializer.Deserialize<StoredMessageHeader>(messageHeaderJson, options)
            ?? new StoredMessageHeader();

        var properties = CreateBasicProperties(payload);
        return new MessageHeader
        {
            Id = payload.Id,
            Timestamp = payload.Timestamp,
            AppId = payload.AppId,
            ContentType = payload.ContentType,
            Type = payload.Type,
            Exchange = payload.Exchange,
            RoutingKey = payload.RoutingKey,
            Properties = properties,
        };
    }

    public static BasicProperties CreateBasicProperties(MessageHeader messageHeader)
    {
        var properties = CreateBasicProperties(messageHeader.Properties as IReadOnlyBasicProperties);

        if (string.IsNullOrWhiteSpace(properties.MessageId))
        {
            properties.MessageId = messageHeader.Id;
        }

        if (string.IsNullOrWhiteSpace(properties.AppId))
        {
            properties.AppId = messageHeader.AppId;
        }

        if (string.IsNullOrWhiteSpace(properties.ContentType))
        {
            properties.ContentType = messageHeader.ContentType;
        }

        if (string.IsNullOrWhiteSpace(properties.Type))
        {
            properties.Type = messageHeader.Type;
        }

        if (properties.Timestamp.UnixTime <= 0 && messageHeader.Timestamp != default)
        {
            properties.Timestamp = new AmqpTimestamp(messageHeader.Timestamp.ToUnixTimeMilliseconds());
        }

        return properties;
    }

    public static string SerializeBody(byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Convert.ToBase64String(body);
    }

    public static byte[] DeserializeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Array.Empty<byte>();
        }

        return Convert.FromBase64String(body);
    }

    private static BasicProperties CreateBasicProperties(StoredMessageHeader payload)
    {
        var properties = CreateBasicProperties(payload.Properties);
        properties.MessageId = string.IsNullOrWhiteSpace(properties.MessageId) ? payload.Id : properties.MessageId;
        properties.AppId = string.IsNullOrWhiteSpace(properties.AppId) ? payload.AppId : properties.AppId;
        properties.ContentType = string.IsNullOrWhiteSpace(properties.ContentType) ? payload.ContentType : properties.ContentType;
        properties.Type = string.IsNullOrWhiteSpace(properties.Type) ? payload.Type : properties.Type;

        if (properties.Timestamp.UnixTime <= 0 && payload.Timestamp != default)
        {
            properties.Timestamp = new AmqpTimestamp(payload.Timestamp.ToUnixTimeMilliseconds());
        }

        return properties;
    }

    private static BasicProperties CreateBasicProperties(IReadOnlyBasicProperties? source)
    {
        var properties = new BasicProperties
        {
            DeliveryMode = source?.DeliveryMode ?? DeliveryModes.Persistent,
            MessageId = source?.MessageId,
            AppId = source?.AppId,
            ClusterId = source?.ClusterId,
            ContentEncoding = source?.ContentEncoding,
            ContentType = source?.ContentType,
            CorrelationId = source?.CorrelationId,
            Expiration = source?.Expiration,
            Priority = source?.Priority ?? default,
            ReplyTo = source?.ReplyTo,
            Type = source?.Type,
            UserId = source?.UserId,
            Timestamp = source?.Timestamp ?? default,
        };

        if (source?.Headers != null && source.Headers.Count > 0)
        {
            properties.Headers = new Dictionary<string, object?>(source.Headers.Count);
            foreach (var item in source.Headers)
            {
                properties.Headers[item.Key] = CloneHeaderValue(item.Value);
            }
        }
        else
        {
            properties.Headers = new Dictionary<string, object?>();
        }

        return properties;
    }

    private static StoredBasicProperties? CreateStoredBasicProperties(IReadOnlyBasicProperties? properties)
    {
        if (properties == null)
        {
            return null;
        }

        Dictionary<string, StoredTableValue>? headers = null;
        if (properties.Headers != null && properties.Headers.Count > 0)
        {
            headers = new Dictionary<string, StoredTableValue>(properties.Headers.Count);
            foreach (var item in properties.Headers)
            {
                headers[item.Key] = ToStoredTableValue(item.Value);
            }
        }

        return new StoredBasicProperties
        {
            DeliveryMode = properties.DeliveryMode,
            MessageId = properties.MessageId,
            AppId = properties.AppId,
            ClusterId = properties.ClusterId,
            ContentEncoding = properties.ContentEncoding,
            ContentType = properties.ContentType,
            CorrelationId = properties.CorrelationId,
            Expiration = properties.Expiration,
            Priority = properties.Priority,
            ReplyTo = properties.ReplyTo,
            Type = properties.Type,
            UserId = properties.UserId,
            TimestampUnixTime = properties.Timestamp.UnixTime <= 0 ? null : properties.Timestamp.UnixTime,
            Headers = headers,
        };
    }

    private static BasicProperties CreateBasicProperties(StoredBasicProperties? stored)
    {
        var properties = new BasicProperties
        {
            DeliveryMode = stored?.DeliveryMode ?? DeliveryModes.Persistent,
            MessageId = stored?.MessageId,
            AppId = stored?.AppId,
            ClusterId = stored?.ClusterId,
            ContentEncoding = stored?.ContentEncoding,
            ContentType = stored?.ContentType,
            CorrelationId = stored?.CorrelationId,
            Expiration = stored?.Expiration,
            Priority = stored?.Priority ?? default,
            ReplyTo = stored?.ReplyTo,
            Type = stored?.Type,
            UserId = stored?.UserId,
            Timestamp = stored?.TimestampUnixTime is long unixTime ? new AmqpTimestamp(unixTime) : default,
        };

        if (stored?.Headers != null && stored.Headers.Count > 0)
        {
            properties.Headers = new Dictionary<string, object?>(stored.Headers.Count);
            foreach (var item in stored.Headers)
            {
                properties.Headers[item.Key] = FromStoredTableValue(item.Value);
            }
        }
        else
        {
            properties.Headers = new Dictionary<string, object?>();
        }

        return properties;
    }

    private static StoredTableValue ToStoredTableValue(object? value)
    {
        if (value == null)
        {
            return new StoredTableValue { Kind = "null" };
        }

        return value switch
        {
            string text => new StoredTableValue { Kind = "string", StringValue = text },
            bool b => new StoredTableValue { Kind = "bool", BoolValue = b },
            byte b => new StoredTableValue { Kind = "byte", LongValue = b },
            sbyte sb => new StoredTableValue { Kind = "sbyte", LongValue = sb },
            short s => new StoredTableValue { Kind = "short", LongValue = s },
            ushort us => new StoredTableValue { Kind = "ushort", LongValue = us },
            int i => new StoredTableValue { Kind = "int", LongValue = i },
            uint ui => new StoredTableValue { Kind = "uint", ULongValue = ui },
            long l => new StoredTableValue { Kind = "long", LongValue = l },
            ulong ul => new StoredTableValue { Kind = "ulong", ULongValue = ul },
            float f => new StoredTableValue { Kind = "float", DoubleValue = f },
            double d => new StoredTableValue { Kind = "double", DoubleValue = d },
            decimal m => new StoredTableValue { Kind = "decimal", StringValue = m.ToString(CultureInfo.InvariantCulture) },
            byte[] bytes => new StoredTableValue { Kind = "bytes", BytesValue = Convert.ToBase64String(bytes) },
            AmqpTimestamp ts => new StoredTableValue { Kind = "timestamp", LongValue = ts.UnixTime },
            IDictionary<string, object?> table => new StoredTableValue
            {
                Kind = "table",
                TableValue = table.ToDictionary(static x => x.Key, static x => ToStoredTableValue(x.Value)),
            },
            IList list => new StoredTableValue
            {
                Kind = "array",
                ArrayValue = list.Cast<object?>().Select(ToStoredTableValue).ToList(),
            },
            _ => new StoredTableValue { Kind = "string", StringValue = value.ToString() ?? string.Empty },
        };
    }

    private static object? FromStoredTableValue(StoredTableValue value)
    {
        return value.Kind switch
        {
            "null" => null,
            "string" => value.StringValue ?? string.Empty,
            "bool" => value.BoolValue ?? false,
            "byte" => (byte)(value.LongValue ?? 0),
            "sbyte" => (sbyte)(value.LongValue ?? 0),
            "short" => (short)(value.LongValue ?? 0),
            "ushort" => (ushort)(value.LongValue ?? 0),
            "int" => (int)(value.LongValue ?? 0),
            "uint" => (uint)(value.ULongValue ?? 0),
            "long" => value.LongValue ?? 0L,
            "ulong" => value.ULongValue ?? 0UL,
            "float" => (float)(value.DoubleValue ?? 0D),
            "double" => value.DoubleValue ?? 0D,
            "decimal" => decimal.TryParse(value.StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) ? number : decimal.Zero,
            "bytes" => string.IsNullOrWhiteSpace(value.BytesValue) ? Array.Empty<byte>() : Convert.FromBase64String(value.BytesValue),
            "timestamp" => new AmqpTimestamp(value.LongValue ?? 0L),
            "table" => value.TableValue?.ToDictionary(static x => x.Key, static x => FromStoredTableValue(x.Value)) ?? new Dictionary<string, object?>(),
            "array" => value.ArrayValue?.Select(FromStoredTableValue).ToList() ?? new List<object?>(),
            _ => value.StringValue ?? string.Empty,
        };
    }

    private static object? CloneHeaderValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is byte[] bytes)
        {
            return bytes.ToArray();
        }

        if (value is IDictionary<string, object?> table)
        {
            return table.ToDictionary(static x => x.Key, static x => CloneHeaderValue(x.Value));
        }

        if (value is IList list)
        {
            return list.Cast<object?>().Select(CloneHeaderValue).ToList();
        }

        return value;
    }

    private sealed class StoredMessageHeader
    {
        public string Id { get; set; } = string.Empty;

        public DateTimeOffset Timestamp { get; set; }

        public string AppId { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Exchange { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;

        public StoredBasicProperties? Properties { get; set; }
    }

    private sealed class StoredBasicProperties
    {
        public string? MessageId { get; set; }

        public string? AppId { get; set; }

        public string? ClusterId { get; set; }

        public string? ContentEncoding { get; set; }

        public string? ContentType { get; set; }

        public string? CorrelationId { get; set; }

        public string? Expiration { get; set; }

        public byte? Priority { get; set; }

        public string? ReplyTo { get; set; }

        public DeliveryModes DeliveryMode { get; set; }

        public string? Type { get; set; }

        public string? UserId { get; set; }

        public long? TimestampUnixTime { get; set; }

        public Dictionary<string, StoredTableValue>? Headers { get; set; }
    }

    private sealed class StoredTableValue
    {
        public string Kind { get; set; } = "null";

        public string? StringValue { get; set; }

        public bool? BoolValue { get; set; }

        public long? LongValue { get; set; }

        public ulong? ULongValue { get; set; }

        public double? DoubleValue { get; set; }

        public string? BytesValue { get; set; }

        public Dictionary<string, StoredTableValue>? TableValue { get; set; }

        public List<StoredTableValue>? ArrayValue { get; set; }
    }
}
