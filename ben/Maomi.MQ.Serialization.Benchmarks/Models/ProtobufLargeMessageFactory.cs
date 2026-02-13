using System.Linq.Expressions;
using System.Reflection;
using Maomi.MQ.Serialization.Benchmarks.Proto;

namespace Maomi.MQ.Serialization.Benchmarks.Models;

internal static class ProtobufLargeMessageFactory
{
    private const int FieldCount = 100;
    private static readonly Action<PersonLarge100, int>[] Setters = CreateSetters();

    public static PersonLarge100 CreateSample()
    {
        var message = new PersonLarge100();
        for (var index = 1; index <= FieldCount; index++)
        {
            Setters[index](message, index);
        }

        return message;
    }

    private static Action<PersonLarge100, int>[] CreateSetters()
    {
        var setters = new Action<PersonLarge100, int>[FieldCount + 1];
        for (var index = 1; index <= FieldCount; index++)
        {
            var propertyName = $"F{index:D3}";
            var property = typeof(PersonLarge100).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on '{nameof(PersonLarge100)}'.");

            var messageParameter = Expression.Parameter(typeof(PersonLarge100), "message");
            var valueParameter = Expression.Parameter(typeof(int), "value");
            var assignment = Expression.Assign(Expression.Property(messageParameter, property), valueParameter);
            setters[index] = Expression.Lambda<Action<PersonLarge100, int>>(assignment, messageParameter, valueParameter).Compile();
        }

        return setters;
    }
}
