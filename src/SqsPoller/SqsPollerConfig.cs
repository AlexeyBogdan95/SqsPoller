using System.Collections.Generic;

namespace SqsPoller
{
    public class SqsPollerConfig
    {
        public string Region { get; set; }
        
        public string ServiceUrl { get; set; }
        
        public string QueueUrl { get; set; }

        /// <summary>
        /// Gets and sets the property WaitTimeSeconds (default 20).
        /// <para>
        /// The duration (in seconds) for which the call waits for a message to arrive in the
        /// queue before returning. If a message is available, the call returns sooner than <code>WaitTimeSeconds</code>.
        /// If no messages are available and the wait time expires, the call returns successfully
        /// with an empty list of messages.
        /// </para>
        /// </summary>
        public int WaitTimeSeconds { get; set; } = 20;

        /// <summary>
        /// Gets and sets the property MaxNumberOfMessages (default 1).
        /// <para>
        /// The maximum number of messages to return. Amazon SQS never returns more messages than
        /// this value (however, fewer messages might be returned). Valid values: 1 to 10.
        /// </para>
        /// </summary>
        public int MaxNumberOfMessages { get; set; } = 1;
        
        /// <summary>
        /// Gets and sets the property AttributeNames.
        /// </summary>
        /// <inheritdoc cref="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-message-attributes.html"/>
        public List<string> MessageAttributeNames { get; set; } = new List<string> {"All", ".*"};
    }
}