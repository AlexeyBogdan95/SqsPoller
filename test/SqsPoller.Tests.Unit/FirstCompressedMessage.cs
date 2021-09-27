namespace SqsPoller.Tests.Unit
{
    public record FirstCompressedMessage
    {
        public string FirstValue { get; set; } = string.Empty;
        public int SecondValue { get; set; }
    }
}
