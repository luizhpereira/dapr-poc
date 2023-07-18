using WebFrontEnd;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Dapr.Client;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;

namespace WebFrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        const string storeName = "statestore";
        const string key = "weighing";

        private readonly DaprClient _daprClient;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }
        public async Task OnGet()
        {
            /* Weighing implementation */
            var lastWeighingRegistries = await _daprClient.GetStateAsync<IEnumerable<WeighingRegistry>>(storeName, key);

            var weighingRegistries = await _daprClient.InvokeMethodAsync<IEnumerable<WeighingRegistry>>(
            HttpMethod.Get, "WebAPI", "WeighingRegistry/GetRandomWeigh");

            ViewData["LastWeighingRegistries"] = lastWeighingRegistries;
            ViewData["WeighingRegistriesData"] = weighingRegistries;

            await _daprClient.SaveStateAsync(storeName, key, weighingRegistries);
            Console.WriteLine(_logger);

            /* gRPC implementation */
            var services = new ServiceCollection();

            using var channel = GrpcChannel.ForAddress("http://localhost:50001", new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                ServiceProvider = services.BuildServiceProvider(),
                ServiceConfig = new ServiceConfig(),
                HttpHandler = new SocketsHttpHandler()
            });

            var client = new Greeter.GreeterClient(channel);

            var request = new HelloRequest { Name = "Cachapuz - Bilanciai Group"};            

            var response = await client.SayHelloAsync(request, headers: BuildMetadataHeader());

            Console.WriteLine(response.Message);

            List<HelloReply> replyList = new List<HelloReply>();
            replyList.Add(response);

            ViewData["gRPCReply"] = replyList;

            /* gRPC Dapr */
            //var request = new HelloRequest { Name = "Luiz Pereira" };
            //var reply = await _daprClient.InvokeMethodGrpcAsync<HelloRequest, HelloReply>("GrpcServer", "SayHello", request).ConfigureAwait(false);
        }

        Metadata? BuildMetadataHeader()
        {
            var daprGRPCPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT");
            daprGRPCPort = "50001";

            Metadata? metadata = null;

            //if (!string.IsNullOrEmpty(daprGRPCPort))
            //{
                metadata = new Metadata();
                var serverDaprAppId = "GrpcServer";
                metadata.Add("dapr-app-id", serverDaprAppId);
                _logger.LogInformation("Calling gRPC server app id '{server}' using dapr sidecar on gRPC port: {daprGRPCPort}", serverDaprAppId, daprGRPCPort);
            //}

            return metadata;
        }

    }
}