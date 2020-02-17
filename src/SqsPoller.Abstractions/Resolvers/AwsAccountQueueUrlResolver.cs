using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;

namespace SqsPoller.Abstractions.Resolvers
{
    public class AwsAccountQueueUrlResolver : IQueueUrlResolver
    {
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly string _queueName;

        private string BaseUrl { get; set; }

        public AwsAccountQueueUrlResolver(IAmazonSQS amazonSqsClient, string queueName)
        {
            _amazonSqsClient = amazonSqsClient;
            _queueName = queueName;
        }

        public async Task<string> Resolve(CancellationToken cancellationToken = default)
        {
            // Prefered way of making SQS queue URLs - no Amazon SQS Requests fees for GetQueueUrl/CreateQueue requests
            if (!string.IsNullOrEmpty(BaseUrl))
            {
                var uri = new Uri(new Uri(BaseUrl), _queueName);
                return uri.AbsoluteUri;
            }

            var queueUrlResponse = await _amazonSqsClient.GetQueueUrlAsync(_queueName, cancellationToken);
            BaseUrl = RemoveFromEnd(queueUrlResponse.QueueUrl, _queueName);
            return queueUrlResponse.QueueUrl;
        }
        
        private string RemoveFromEnd(string queueUrl, string queueName)
        {
            if (queueUrl.EndsWith(queueName))
            {
                return queueUrl.Substring(0, queueUrl.Length - queueName.Length);
            }
            else
            {
                return queueUrl;
            }
        }
    }
}