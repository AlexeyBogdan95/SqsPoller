using System;

namespace SqsPoller
{
    public class ConsumerNotFoundException: Exception
    {
        public ConsumerNotFoundException(string messageType) :
            base($"There are no consumers that can consume {messageType} message")
        {
        }
    }
}