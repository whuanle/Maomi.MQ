namespace ConsumerWeb.Models;

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}


public class DeadEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}

public class DeadQueueEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
