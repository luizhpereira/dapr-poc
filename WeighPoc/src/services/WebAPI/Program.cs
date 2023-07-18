using Microsoft.AspNetCore.Http.Json;
using WebAPI;
using WebAPI.Helper;
using WebAPI.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDaprClient();
//builder.Services.AddScoped<IPubSubMessageHelper, PubSubMessageHelper>();
//builder.Services.AddScoped<IDaprClientHelper, DaprClientHelper>();
builder.Services.AddControllers();
//builder.Services.AddControllers().AddJsonOptions(options =>
//    options.JsonSerializerOptions.PropertyNamingPolicy = null
//).AddDapr();


/* implementa��o do registo de atores */
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<WeighingActor>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

/* mapeia os identificadores dos atores */
app.MapActorsHandlers();

app.MapControllers();

/* m�todo nativo de comunica��o pub-sub dapr */
app.UseCloudEvents();

/* mapeia as subscri��es feitas [pub-sub] */
app.MapSubscribeHandler();

app.Run();
