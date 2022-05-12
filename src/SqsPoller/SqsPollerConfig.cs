using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SqsPoller
{
    public class SqsPollerConfig
    {
        /// <summary>
        /// Gets or sets AWS Region
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Gets and sets of the ServiceURL property.
        /// This is an optional property; change it
        /// only if you want to try a different service
        /// endpoint.
        /// </summary>
        public string ServiceUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets AWS SQS Queue URL. It overrides QueueName.
        /// </summary>
        public string QueueUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets AWS SQS Queue Name. It's overriden by QueueUrl
        /// </summary>
        public string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets AWS Access Key
        /// </summary>
        public string AccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets AWS Secret Key
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

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

        /// <summary>
        /// Gets and sets the max number of concurrent message handlers (default 100).
        /// </summary>
        public int MaxNumberOfParallelism { get; set; } = 100;

        /// <summary>
        /// Gets and sets a log level for default log messages when an exception is caught
        /// </summary>
        public LogLevel ExceptionDefaultMessageLogLevel { get; set; } = LogLevel.Error;
        
        /// <summary>
        /// Gets and sets a custom exception handler when it is caught.
        /// Default log message is applied.
        /// </summary>
        public Action<Exception>? OnException { get; set; }
    }
}