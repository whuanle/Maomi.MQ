namespace ConsumerWeb.Models;

public class DeadEvent
{
    public int Id { get; set; }
    public override string ToString()
    {
        return Id.ToString();
    }
}
