using System.Text.Json.Nodes;

namespace WebAPI.Interfaces
{
    public interface IPubSubMessageHelper
    {
        Task<string> GetMessage();
        Task<bool> PublishMessageTopic(string attribute, string asset, JsonObject data);
    }
}
