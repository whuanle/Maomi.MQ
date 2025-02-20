namespace PublisherWeb.MQ;

public class TestMessageEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
