using System.Text.Json.Nodes;

namespace WebAPI
{
    public class MqttOpenRemoteHelper
    {
        public string Attribute { get; set; } = string.Empty;

        public string Asset { get; set; } = string.Empty;

        public JsonObject Data { get; set; } = new JsonObject();
    }
}
