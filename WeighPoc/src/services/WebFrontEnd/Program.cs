using WebFrontEnd;
using Grpc.Net.Client;
using Grpc.Core;
using Google.Api;
using Grpc.Net.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDaprClient();
//builder.Services.AddGrpcClient<Greeter.GreeterClient>();
//builder.Services.AddGrpcClient<Greeter.GreeterClient>(o =>
//{
//var isLocalhost = builder.Configuration.GetValue("grpc:localhost", false);

//if (isLocalhost)
//{
//    var port = "3501";
//    var scheme = "https";
//    var daprGRPCPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT");

//    if (!string.IsNullOrEmpty(daprGRPCPort))
//    {
//        scheme = "http";
//        port = daprGRPCPort;
//    }
//    serverAddress = string.Format(builder.Configuration.GetValue<string>("grpc:server"), scheme, port);
//}
//else
//{
//    serverAddress = builder.Configuration.GetValue<string>("grpc:server"); ;
//}
//    o.Address = new Uri(serverAddress);
//});

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

/* implementação para pubsub */
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapRazorPages();

app.Run();