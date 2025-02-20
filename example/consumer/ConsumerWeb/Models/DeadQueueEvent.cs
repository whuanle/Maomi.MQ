namespace ConsumerWeb.Models;

public class DeadQueueEvent
{
    public int Id { get; set; }
    public override string ToString()
    {
        return Id.ToString();
    }
}