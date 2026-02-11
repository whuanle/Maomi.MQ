// <copyright file="TransactionConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Extensions for DbCommand to support Maomi MQ transaction operations.
/// </summary>
public static class DbCommandExtensions
{
    /// <summary>
    /// Asynchronously executes a SQL command and returns the first column of the first row in the result set returned by the query.
    /// </summary>
    /// <typeparam name="T">.</typeparam>
    /// <param name="command"></param>
    /// <param name="sql"></param>
    /// <returns>T.</returns>
    public static async Task<T?> QueryScalarAsync<T>(this DbCommand command, string? sql = null)
    {
        if (!string.IsNullOrEmpty(sql))
        {
            command.CommandText = sql;
        }

        var typeCode = Type.GetTypeCode(typeof(T));
        if (typeCode == TypeCode.Empty)
        {
            // 不支持
            throw new NotSupportedException("TypeCode.Empty is not supported.");
        }

        if (typeCode == TypeCode.Object)
        {
            return await command.QueryScalarClassAsync<T>();
        }

        if ((int)typeCode <= 18 || typeof(T) == typeof(DateTimeOffset))
        {
            return await command.QueryScalarStructAsync<T>();
        }

        return default;
    }

    /// <summary>
    /// Asynchronously executes a SQL command and returns a list of results.
    /// </summary>
    /// <typeparam name="T">.</typeparam>
    /// <param name="command"></param>
    /// <param name="sql"></param>
    /// <returns>T.</returns>
    public static async Task<IReadOnlyList<T>> QueryListAsync<T>(this DbCommand command, string? sql = null)
    {
        if (!string.IsNullOrEmpty(sql))
        {
            command.CommandText = sql;
        }

        var typeCode = Type.GetTypeCode(typeof(T));
        if (typeCode == TypeCode.Empty)
        {
            throw new NotSupportedException("TypeCode.Empty is not supported.");
        }

        if (typeCode == TypeCode.Object)
        {
            return await command.QueryStructListAsync<T>();
        }

        if ((int)typeCode <= 18 || typeof(T) == typeof(DateTimeOffset))
        {
            return await command.QueryClassListAsync<T>();
        }

        return new List<T>();
    }

    /// <summary>
    /// Asynchronously inserts an entity into the database using the provided DbCommand.
    /// </summary>
    /// <typeparam name="T">Entity.</typeparam>
    /// <param name="cmd"></param>
    /// <param name="entity"></param>
    /// <param name="tableName"></param>
    /// <param name="primaryKey"></param>
    /// <returns>Task.</returns>
    public static async Task InsertAsync<T>(this DbCommand cmd, T entity, string tableName, string? primaryKey = null)
        where T : new()
    {
        cmd.Parameters.Clear();
        List<string> columnNames = new();
        List<string> values = new();

        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (primaryKey != null && property.Name == primaryKey)
            {
                continue;
            }

            columnNames.Add(property.Name);
            values.Add($"{property.Name} = @{property.Name}");
        }

        cmd.CommandText = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", values)})";

        foreach (var property in properties)
        {
            if (primaryKey != null && property.Name == primaryKey)
            {
                continue;
            }

            var value = property.GetValue(entity);
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + property.Name;
            p.Value = value ?? DBNull.Value;

            if (value != null)
            {
                p.DbType = ConvertToDbType(value);
            }

            cmd.Parameters.Add(p);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Asynchronously updates an entity in the database using the provided DbCommand.
    /// </summary>
    /// <typeparam name="T">Entity.</typeparam>
    /// <param name="cmd"></param>
    /// <param name="entity"></param>
    /// <param name="tableName"></param>
    /// <param name="primaryKey"></param>
    /// <returns>Task.</returns>
    public static async Task UpdateAsync<T>(this DbCommand cmd, T entity, string tableName, string primaryKey)
    {
        var properties = typeof(T).GetProperties();
        var setClauses = new List<string>();
        object primaryKeyValue = null!;

        cmd.Parameters.Clear();

        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            if (primaryKey != null && property.Name.Equals(primaryKey, StringComparison.OrdinalIgnoreCase))
            {
                primaryKeyValue = value!;
                continue;
            }

            setClauses.Add($"{property.Name} = @{property.Name}");
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + property.Name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        if (primaryKeyValue == null)
        {
            throw new ArgumentNullException(nameof(primaryKeyValue), "Primary key value cannot be null.");
        }

        var pkParam = cmd.CreateParameter();
        pkParam.ParameterName = "@pk";
        pkParam.Value = primaryKeyValue;
        cmd.Parameters.Add(pkParam);

        cmd.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {primaryKey} = @pk";

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T?> QueryScalarStructAsync<T>(this DbCommand command)
    {
        var result = await command.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)result.ToString()!;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private static async Task<T?> QueryScalarClassAsync<T>(this DbCommand command)
    {
        using var reader = await command.ExecuteReaderAsync();
        var properties = typeof(T).GetProperties();
        if (!reader.HasRows)
        {
            return default;
        }

        while (reader.Read())
        {
            var entity = Activator.CreateInstance<T>();
            foreach (var property in properties)
            {
                if (reader[property.Name] == DBNull.Value)
                {
                    continue;
                }

                var value = reader[property.Name];
                value = ConvertDbValue(property, value);

                property.SetValue(entity, value);
            }

            return entity;
        }

        return default;
    }

    private static async Task<List<T>> QueryStructListAsync<T>(this DbCommand cmd)
    {
        var results = new List<T>();

        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return results;
        }

        while (reader.Read())
        {
            results.Add(reader.GetFieldValue<T>(0));
            reader.NextResult();
        }

        return results;
    }

    private static async Task<List<T>> QueryClassListAsync<T>(this DbCommand cmd)
    {
        var results = new List<T>();

        using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return results;
        }

        var type = typeof(T);

        var properties = type.GetProperties();
        var columnNames = reader.GetColumnSchema()
            .Select(c => c.ColumnName)
            .ToList();

        while (reader.Read())
        {
            var entity = Activator.CreateInstance<T>();

            foreach (var property in properties)
            {
                if (!columnNames.Contains(property.Name))
                {
                    continue;
                }

                var value = reader[property.Name];
                if (value == DBNull.Value)
                {
                    continue;
                }

                value = ConvertDbValue(property, value);
                property.SetValue(entity, value);
            }

            results.Add(entity);
            reader.NextResult();
        }

        return results;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ConvertDbValue(PropertyInfo property, object? value)
    {
        if (value == null)
        {
            return default;
        }

        if (value != null && property.PropertyType.IsEnum)
        {
            value = Enum.ToObject(property.PropertyType, value);
        }
        else if (value != null && property.PropertyType == typeof(DateTime))
        {
            value = Convert.ToDateTime(value);
        }
        else if (value != null && property.PropertyType == typeof(Guid))
        {
            value = Guid.Parse(value.ToString()!);
        }
        else if (value != null && property.PropertyType == typeof(bool))
        {
            value = Convert.ToBoolean(value);
        }
        else if (value != null && property.PropertyType == typeof(int))
        {
            value = Convert.ToInt32(value);
        }
        else if (value != null && property.PropertyType == typeof(long))
        {
            value = Convert.ToInt64(value);
        }
        else if (value != null && property.PropertyType == typeof(double))
        {
            value = Convert.ToDouble(value);
        }
        else if (value != null && property.PropertyType == typeof(decimal))
        {
            value = Convert.ToDecimal(value);
        }
        else if (value != null && property.PropertyType == typeof(string))
        {
            value = value.ToString();
        }
        else if (value != null && property.PropertyType == typeof(byte[]))
        {
            value = (byte[])value;
        }
        else if (value != null && property.PropertyType == typeof(DateTimeOffset))
        {
            value = DateTimeOffset.Parse(value.ToString()!);
        }

        return value;
    }

    private static DbType ConvertToDbType(object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Value cannot be null");
        }

        Type type = value.GetType();

        // 使用 switch 或 if ... else 结构判断类型
        if (type == typeof(string))
        {
            return DbType.String;
        }
        else if (type == typeof(int))
        {
            return DbType.Int32;
        }
        else if (type == typeof(short))
        {
            return DbType.Int16;
        }
        else if (type == typeof(long))
        {
            return DbType.Int64;
        }
        else if (type == typeof(byte))
        {
            return DbType.Byte;
        }
        else if (type == typeof(bool))
        {
            return DbType.Boolean;
        }
        else if (type == typeof(decimal))
        {
            return DbType.Decimal;
        }
        else if (type == typeof(double))
        {
            return DbType.Double;
        }
        else if (type == typeof(float))
        {
            return DbType.Single;
        }
        else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return DbType.DateTime;
        }
        else if (type == typeof(Guid))
        {
            return DbType.Guid;
        }
        else if (type == typeof(byte[]))
        {
            return DbType.Binary;
        }
        else
        {
            throw new ArgumentException("Unsupported type: " + type.Name);
        }
    }
}
