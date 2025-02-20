namespace ConsumerWeb.Models;

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
