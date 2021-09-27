using System;

namespace SqsPoller.Tests.Unit
{
    public record SecondCompressedMessage
    {
        public DateTime FirstValue { get; set; }
        public decimal SecondValue { get; set; }
    }
}
