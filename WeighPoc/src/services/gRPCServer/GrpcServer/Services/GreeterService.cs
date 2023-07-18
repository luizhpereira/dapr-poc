using GrpcServer;
using Dapr.Client;
using Grpc.Core;
//using Dapr.AppCallback.Autogen.Grpc.v1;
//using Dapr.Client.Autogen.Grpc.v1;
//using Google.Protobuf.WellKnownTypes;

namespace GrpcServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    //public class GreeterService : AppCallback.AppCallbackBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly DaprClient _daprClient;

        public GreeterService(ILogger<GreeterService> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;

        }

        //public override async Task<InvokeResponse> OnInvoke(InvokeRequest request, ServerCallContext context)
        //{
        //    InvokeResponse response = new();
        //    switch (request.Method)
        //    {
        //        case "SayHello":
        //            HelloRequest input = request.Data.Unpack<HelloRequest>();
        //            HelloReply output = await Task.FromResult(new HelloReply() { Message = "Hello " + input.Name });
        //            response.Data = Any.Pack(output);
        //            break;
        //        default:
        //            Console.WriteLine("Method not supported");
        //            break;
        //    }

        //    return response;
        //}

        public override async Task SayHelloStream(
            IAsyncStreamReader<HelloRequest> request,
            IServerStreamWriter<HelloReply> response,
            ServerCallContext context)
        {

            await foreach(var req in request.ReadAllAsync())
            {
                Console.WriteLine(req);
                await SayHelloStreamServerToClient(request, response, context);
            }
        }

        private async Task SayHelloStreamServerToClient(
            IAsyncStreamReader<HelloRequest> request,
            IServerStreamWriter<HelloReply> response, 
            ServerCallContext context)
        {
            if (!context.CancellationToken.IsCancellationRequested && request.Current.Surname == string.Empty)
            {
                await response.WriteAsync(new HelloReply
                {
                    Message = $"Olá {request.Current.Name}, poderia me dizer seu apelido também no parâmetro surname?"
                });
            } else if (context.CancellationToken.IsCancellationRequested || (request.Current.Surname != string.Empty && request.Current.Name != string.Empty))
            {
                await response.WriteAsync(new HelloReply
                {
                    Message = $"Olá {request.Current.Name} {request.Current.Surname}"
                });
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            }
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Saying hello to {Name}", request.Name);
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}