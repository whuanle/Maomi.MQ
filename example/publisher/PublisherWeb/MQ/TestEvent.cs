namespace PublisherWeb.MQ;

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
