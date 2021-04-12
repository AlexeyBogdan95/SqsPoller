using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SqsPoller.Sample.Subscriber
{
    public class HttpMessage
    {
        public int Value { get; set; }
    }
    
    public class HttpConsumer: IConsumer<HttpMessage>
    {
        private static HttpClient _client = new HttpClient();
        
        public Task Consume(HttpMessage message, CancellationToken cancellationToken)
        {
            return _client.GetAsync(new Uri("https://google.com"), cancellationToken);
        }
    }
}