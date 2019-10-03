using System;

namespace SqsPooler
{
    public class ConsumerNotFoundException: Exception
    {
        public ConsumerNotFoundException(string messageType) :
            base($"There are no consumers that can consume {messageType} message")
        {
        }
    }
}