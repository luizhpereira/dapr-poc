using Dapr.Client;
using WebAPI.Interfaces;

namespace WebAPI.Helper
{
    public class DaprClientHelper : IDaprClientHelper
    {
        private readonly DaprClient _daprClient;

        public DaprClientHelper(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<bool> PublishToTopicWithDaprClient<T>(string pubsubName, string topicName, T data)
        {
            await _daprClient.PublishEventAsync<T>(pubsubName, topicName, data);
            return true;
        }

        public async Task<T> ResponseByDaprClient<T>(HttpMethod httpMethod, string appId, string endPoint)
        {
            HttpRequestMessage daprRequest = _daprClient.CreateInvokeMethodRequest(httpMethod, appId, endPoint);
            var result = await _daprClient.InvokeMethodAsync<T>(daprRequest);
            return result;

        }
    }
}
