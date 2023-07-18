using GrpcServer;
using Dapr.Client;
using Grpc.Core;

namespace GrpcServer.Services
{
    public class WeighingService : Weighing.WeighingBase
    {
        private readonly ILogger<WeighingService> _logger;
        private readonly DaprClient _daprClient;

        public WeighingService(ILogger<WeighingService> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        public override async Task SetWeighingStream(
            IAsyncStreamReader<WeighingRequest> request,
            IServerStreamWriter<WeighingReply> response,
            ServerCallContext context)
        {
            _logger.LogInformation("WEIGHING SERVICE TASK STARTED");

            await foreach (var req in request.ReadAllAsync())
            {
                var strTenant = context.RequestHeaders.GetValue("tenant");
                var strKiosk = context.RequestHeaders.GetValue("kiosk");

                if (!context.CancellationToken.IsCancellationRequested && (string.IsNullOrEmpty(strTenant) || string.IsNullOrEmpty(strKiosk)))
                {
                    _logger.LogInformation($"NO HEADERS: TENANT ({strTenant}) or KIOSK ({strKiosk})");
                    await NoHeader(response, context);

                }
                else if (!context.CancellationToken.IsCancellationRequested && (!string.IsNullOrEmpty(strTenant) && !string.IsNullOrEmpty(strKiosk)))
                {
                    if (!context.CancellationToken.IsCancellationRequested && req.Weigh > 0 && !string.IsNullOrEmpty(req.Print))
                    {
                        await WeighCheck(strTenant, strKiosk, req, response, context);

                    } 
                    else if (!context.CancellationToken.IsCancellationRequested && string.IsNullOrEmpty(req.Print)) 
                    {
                        await response.WriteAsync(new WeighingReply
                        {
                            Message = "Send the print layout in the 'print' parameter.",
                            Status = "WARNING"
                        });
                    }
                    else if (!context.CancellationToken.IsCancellationRequested && req.Weigh <= 0)
                    {
                        await response.WriteAsync(new WeighingReply
                        {
                            Message = "Weighing value is below or equal 0, inform a valid value.",
                            Status = "WARNING"
                        });
                    }
                }
            }
        }


        private async Task WeighCheck(
            string tenant, 
            string kiosk,
            WeighingRequest request,
            IServerStreamWriter<WeighingReply> response,
            ServerCallContext context)
        {
            try
            {
                //if (request.Weigh > 100) request.State = "VB";
                //if (request.Weigh < 100) request.State = "AG";

                var wObj = new WeighingRegistry
                {
                    Date = DateTime.Now,
                    Weigh = request.Weigh,
                    State = request.State,
                    Tenant = tenant,
                    Kiosk = kiosk
                };

                _ = _daprClient.InvokeMethodAsync<WeighingRegistry, WeighingRegistry>(
                    HttpMethod.Post, "WebAPI", "WeighingRegistry/SetWeigh", wObj);

                await response.WriteAsync(new WeighingReply
                {
                    Message = $"Your context has been saved",
                    Status = "SUCCESS"
                });
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            } 
            catch (Exception ex)
            {
                await response.WriteAsync(new WeighingReply
                {
                    Message = $"Fails to contact the API - ERROR: {ex}",
                    Status = "ERROR"
                });
                throw new Exception(ex.Message);
            }            
        }


        private async Task NoHeader(
            IServerStreamWriter<WeighingReply> response,
            ServerCallContext context) 
        {
            if (!context.CancellationToken.IsCancellationRequested)
            {
                await response.WriteAsync(new WeighingReply
                {
                    Message = "No headers: 'tenant' and 'kiosk'",
                    Status = "ERROR"
                });
            }
        }
    }
}
