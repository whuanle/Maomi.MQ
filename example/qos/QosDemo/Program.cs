public class Program
{
    static async Task Main()
    {
        for(int i = 0; i < 10; i++)
        {
            //var _ = QosConsole.Program.Main();
            var _ = RabbitMQConsole.Program.Main();
        }
        Console.ReadLine();
    }
}