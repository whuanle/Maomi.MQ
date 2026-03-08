namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Contracts;

public sealed class PublishRetryMessageRequest
{
    public string Text { get; set; } = "retry me";

    public bool ForceFail { get; set; } = true;
}
