// <copyright file="ThriftMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Thrift;
using Thrift.Protocol;
using Thrift.Transport.Client;
using System.Linq.Expressions;

namespace Maomi.MQ;

/// <summary>
/// Thrift message serializer.
/// </summary>
public class ThriftMessageSerializer : IMessageSerializer
{
    private static readonly TConfiguration Configuration = new();
    private static readonly TProtocolFactory ProtocolFactory = new TBinaryProtocol.Factory();

    /// <inheritdoc />
    public string ContentType => "application/x-thrift";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!ThriftTypeVerifier<TObject>.IsThriftType)
        {
            throw new ArgumentException("The object must be a thrift message.", typeof(TObject).Name);
        }

        var message = ThriftMessageFactory<TObject>.Factory();

        using var transport = new TMemoryBufferTransport(bytes.ToArray(), Configuration);
        var protocol = ProtocolFactory.GetProtocol(transport);
        message.ReadAsync(protocol, CancellationToken.None).GetAwaiter().GetResult();

        return (TObject?)message;
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        if (obj is not TBase message)
        {
            throw new ArgumentException("The object must be a thrift message.", nameof(obj));
        }

        using var transport = new TMemoryBufferTransport(Configuration, 1024);
        var protocol = ProtocolFactory.GetProtocol(transport);

        message.WriteAsync(protocol, CancellationToken.None).GetAwaiter().GetResult();
        protocol.Transport.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();

        var data = transport.GetBuffer();
        return data.AsSpan(0, transport.Length).ToArray();
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>(TObject obj)
    {
        return obj is TBase;
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>()
    {
        return ThriftTypeVerifier<TObject>.IsThriftType;
    }

    private static bool IsThriftType(Type type)
    {
        return type.IsAssignableTo(typeof(TBase));
    }

    private static Func<TBase> CreateFactory(Type type)
    {
        if (!IsThriftType(type) || type.IsAbstract || type.IsInterface)
        {
            return () => throw new InvalidOperationException($"The thrift message type '{type.FullName}' could not be instantiated.");
        }

        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            return () => throw new InvalidOperationException($"The thrift message type '{type.FullName}' must declare a public parameterless constructor.");
        }

        var newExpression = Expression.New(constructor);
        var convertExpression = Expression.Convert(newExpression, typeof(TBase));
        return Expression.Lambda<Func<TBase>>(convertExpression).Compile();
    }

    private static class ThriftTypeVerifier<TObject>
    {
        public static readonly bool IsThriftType = IsThriftType(typeof(TObject));
    }

    private static class ThriftMessageFactory<TObject>
    {
        public static readonly Func<TBase> Factory = CreateFactory(typeof(TObject));
    }
}
