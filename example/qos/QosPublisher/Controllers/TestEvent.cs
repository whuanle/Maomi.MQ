namespace QosPublisher.Controllers;

public class TestEvent
{
    public int Id { get; set; }
    public string Message { get; set; }
    public int[] Data { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
