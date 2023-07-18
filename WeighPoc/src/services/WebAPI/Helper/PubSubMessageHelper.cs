using System.Text.Json.Nodes;
using WebAPI.Interfaces;


namespace WebAPI.Helper
{
    public class PubSubMessageHelper : IPubSubMessageHelper
    {
        private readonly IDaprClientHelper _daprClientHelper;

        public PubSubMessageHelper(IDaprClientHelper daprClientHelper)
        {
            _daprClientHelper = daprClientHelper;
        }

        public async Task<string> GetMessage()
        {
            var response = await _daprClientHelper.ResponseByDaprClient<string>(HttpMethod.Get, "WebAPI", "WeighingRegistry/GetRandomWeigh");
            return response;
        }

        public async Task<bool> PublishMessageTopic(string attribute, string asset, JsonObject data)
        {
            //ToDo: add company name
            return await _daprClientHelper.PublishToTopicWithDaprClient("mqtt-pubsub", $"<companyName>/webapi-client/writeattributevalue/{attribute}/{asset}", data);
        }

    }
}
