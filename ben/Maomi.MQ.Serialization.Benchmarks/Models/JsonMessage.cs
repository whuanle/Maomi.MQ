namespace Maomi.MQ.Serialization.Benchmarks.Models;

public sealed class JsonMessage
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
