using Dapr.Actors;

namespace WebAPI.Actors
{
    public interface IWeighing : IActor
    {
        Task<bool>VerifyWeighingState(Weighing data);
        Task<string> SetDataAsync(Weighing data);
        Task<Weighing> GetDataAsync();
        Task RegisterReminder();
        Task UnregisterReminder();
        Task RegisterTimer();
        Task UnregisterTimer();
        //Task WeighCheck(DaprClient _daprClient, string store, string key);
    }

    public class Weighing : WeighingRegistry
    {
        //public string? PropertyA { get; set; }
        //public string? PropertyB { get; set; }

        //public override string ToString()
        //{
        //    var propAValue = this.PropertyA == null ? "null" : this.PropertyA;
        //    var propBValue = this.PropertyB == null ? "null" : this.PropertyB;
        //    return $"PropertyA: {propAValue}, PropertyB: {propBValue}";
        //}
    }
}
