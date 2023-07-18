using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using Dapr;
using Dapr.Client;
using Dapr.Actors;
using Dapr.Actors.Client;
using WebAPI.Actors;
using WebAPI.Interfaces;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeighingRegistryController : ControllerBase
    {
        const string storeName = "statestore";
        const string key = "weighing";

        private static readonly string[] Tenants = new[]
        {
        "EmpresaA", "EmpresaB", "EmpresaC"
        };

        private static readonly string[] Kiosks = new[]
        {
        "K1", "K2", "K3", "K4"
        };

        private readonly ILogger<WeighingRegistryController> _logger;
        private readonly DaprClient _daprClient;
        //private readonly IPubSubMessageHelper _pubSubMessageHelper;

        //public WeighingRegistryController(ILogger<WeighingRegistryController> logger, DaprClient daprClient, IPubSubMessageHelper pubSubMessageHelper)
        public WeighingRegistryController(ILogger<WeighingRegistryController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
            //_pubSubMessageHelper = pubSubMessageHelper;
        }

        [HttpGet]
        [Route("GetRandomWeigh")]
        public IEnumerable<WeighingRegistry> GetRandomWeigh()
        {
            Console.WriteLine(_logger);

            return Enumerable.Range(1, 3).Select(x => new WeighingRegistry
            {
                Date = DateTime.Now,
                Weigh = Random.Shared.Next(-20, 46000),
                Tenant = Tenants[Random.Shared.Next(Tenants.Length)],
                State = "XPTO",
                Kiosk = Kiosks[Random.Shared.Next(Kiosks.Length)]
            })
            .ToArray();
        }

        //[HttpPost]
        //[Route("PublishMessage")]
        //public async Task<ActionResult<string>> PublishMessage(MqttOpenRemoteHelper mqttOpenRemote)
        //{
        //    var isPublished = await _pubSubMessageHelper.PublishMessageTopic(mqttOpenRemote.Attribute, mqttOpenRemote.Asset, mqttOpenRemote.Data);
        //    if (!isPublished)
        //    {
        //        return new JsonResult("Failed");
        //    }
        //    return new JsonResult("Message Published");
        //}

        [Topic("mqtt-pubsub", "caztest")]
        [HttpPost(nameof(PublishedMessage))]
        public ActionResult<bool> PublishedMessage(JsonObject data)
        {
            Console.WriteLine(data);
            return true;
        }

        [Topic("mqtt-pubsub", "cachapuz/webapi-client/writeattributevalue/#")]
        [HttpPost(nameof(SetWeigh))]
        public async Task<ActionResult> SetWeigh([NotNull] JsonObject data)
        {
            Console.WriteLine($"DATA RECEIVED: {data}"); /* DEBUG */

            try
            {
                WeighingRegistry weigh = new WeighingRegistry();

                if (data.ContainsKey("Tenant") && data.ContainsKey("Kiosk") && data.ContainsKey("State") && data.ContainsKey("Weigh"))
                {
                    var dataDict = new List<KeyValuePair<string, JsonNode>>(data!);
                    string propDict = string.Empty;
                    
                    propDict = (string?)dataDict.Find(x => x.Key.ToUpper() == "TENANT").Value;
                    weigh.Tenant = propDict != null ? propDict : string.Empty;

                    propDict = (string?)dataDict.Find(x => x.Key.ToUpper() == "KIOSK").Value;
                    weigh.Kiosk = propDict != null ? propDict : string.Empty;

                    propDict = (string?)dataDict.Find(x => x.Key.ToUpper() == "STATE").Value;
                    weigh.State = propDict != null ? propDict : string.Empty;

                    weigh.Weigh = (int)(dataDict.Find(x => x.Key.ToUpper() == "WEIGH").Value);
                } else
                {
                    return new JsonResult($"PROPERTIES DON`T MATCH");
                }

                /* Invoke & State Dapr */
                string wKey = $"{key}/{weigh.Tenant}/{weigh.Kiosk}";

                string lockResponse = await TaskResourceLocker(wKey);

                WeighingRegistry weighingObj = new WeighingRegistry();

                WeighingRegistry lastWeighing = await _daprClient.GetStateAsync<WeighingRegistry>(storeName, wKey);

                weighingObj = weighingObj.ValidateWeighing(weigh);

                await _daprClient.SaveStateAsync(storeName, wKey, weighingObj);

                weighingObj.LastWeighing = new List<WeighingRegistry> { lastWeighing };

                /* implementação do ator */
                string actorId = $"{key}{weigh.Tenant}{weigh.Kiosk}";
                var weighingProcessActor = await GetWeighingValidateProcessActorAsync(actorId);
                await weighingProcessActor.VerifyWeighingState(new Weighing
                {
                    Date = weighingObj.Date,
                    Weigh = weighingObj.Weigh,
                    Tenant = weighingObj.Tenant,
                    State = weighingObj.State,
                    Kiosk = weighingObj.Kiosk
                });

                var jsonResponse = JsonSerializer.Serialize<WeighingRegistry>(weighingObj);

                Console.WriteLine($"RESPONSE : {jsonResponse}"); /* DEBUG */

                return new JsonResult(jsonResponse);

            } catch (Exception ex)
            {
                return new JsonResult($"ERROR: {ex}");
            }
        }

        public Task<IWeighing> GetWeighingValidateProcessActorAsync(string id)
        {
            var actorId = new ActorId(id);
            return Task.FromResult(ActorProxy.Create<IWeighing>(actorId, nameof(WeighingActor)));
        }

        [Obsolete]
        public async Task<string> TaskResourceLocker(string dataId)
        {
            /* Lock implementation*/
            string DAPR_LOCK_NAME = "lockredis";
            string uuidLock = Guid.NewGuid().ToString();

            await using (var fileLock = await _daprClient.Lock(DAPR_LOCK_NAME, dataId, uuidLock, 60))
            {
                if (fileLock.Success)
                {
                    //Thread.Sleep(10000);
                    Console.WriteLine("Success");
                    return await Task.FromResult($"{fileLock.LockOwner}");

                }
                else
                {
                    Console.WriteLine($"Failed to lock {dataId}.");
                    return await Task.FromResult($"Failed to lock {dataId}.");
                }
            }
            /* Lock implementation END */
        }

    }
}
