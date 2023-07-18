using Dapr.Actors.Runtime;
using WebAPI.Actors;
using Dapr.Client;
using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace WebAPI
{
    internal class WeighingActor : Actor, IWeighing, IRemindable
    {
        const string storeName = "statestore";
        const string key = "weighing";

        private readonly DaprClient _daprClient;

        // The constructor must accept ActorHost as a parameter, and can also accept additional
        // parameters that will be retrieved from the dependency injection container
        //
        /// <summary>
        /// Initializes a new instance of Weighing
        /// </summary>
        /// <param name="host">The Dapr.Actors.Runtime.ActorHost that will host this actor instance.</param>
        public WeighingActor(ActorHost host, DaprClient daprClient)
            : base(host)
        {
            _daprClient = daprClient;
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            // Provides opportunity to perform some optional setup.
            Console.WriteLine($"Activating actor id: {this.Id}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is called whenever an actor is deactivated after a period of inactivity.
        /// </summary>
        protected override Task OnDeactivateAsync()
        {
            // Provides Opporunity to perform optional cleanup.
            Console.WriteLine($"Deactivating actor id: {this.Id}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set Weighing into actor's private state store
        /// </summary>
        /// <param name="data">the user-defined Weighing which will be stored into state store as "my_data" state</param>
        public async Task<string> SetDataAsync(Weighing data)
        {
            // Data is saved to configured state store implicitly after each method execution by Actor's runtime.
            // Data can also be saved explicitly by calling this.StateManager.SaveStateAsync();
            // State to be saved must be DataContract serializable.
            await this.StateManager.SetStateAsync<Weighing>(
                "my_data",  // state name
                data);      // data saved for the named state "my_data"

            return "Success";
        }

        /// <summary>
        /// Get Weighing from actor's private state store
        /// </summary>
        /// <return>the user-defined Weighing which is stored into state store as "my_data" state</return>
        public Task<Weighing> GetDataAsync()
        {
            // Gets state from the state store.
            return this.StateManager.GetStateAsync<Weighing>("my_data");
        }

        /// <summary>
        /// Register MyReminder reminder with the actor
        /// </summary>
        public async Task RegisterReminder()
        {
            await this.RegisterReminderAsync(
                "MyReminder",              // The name of the reminder
                null,                      // User state passed to IRemindable.ReceiveReminderAsync()
                TimeSpan.FromSeconds(5),   // Time to delay before invoking the reminder for the first time
                TimeSpan.FromSeconds(5));  // Time interval between reminder invocations after the first invocation
        }

        /// <summary>
        /// Unregister MyReminder reminder with the actor
        /// </summary>
        public Task UnregisterReminder()
        {
            Console.WriteLine("Unregistering MyReminder...");
            return this.UnregisterReminderAsync("MyReminder");
        }

        // <summary>
        // Implement IRemindeable.ReceiveReminderAsync() which is call back invoked when an actor reminder is triggered.
        // </summary>
        public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            Console.WriteLine("ReceiveReminderAsync is called!");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register MyTimer timer with the actor)
        /// </summary>
        public Task RegisterTimer()
        {
            return this.RegisterTimerAsync(
                "MyTimer",                  // The name of the timer
                nameof(this.OnTimerCallBack),       // Timer callback
                null,                       // User state passed to OnTimerCallback()
                TimeSpan.FromSeconds(5),    // Time to delay before the async callback is first invoked
                TimeSpan.FromSeconds(5));   // Time interval between invocations of the async callback
        }

        /// <summary>
        /// Unregister MyTimer timer with the actor
        /// </summary>
        public Task UnregisterTimer()
        {
            Console.WriteLine("Unregistering MyTimer...");
            return this.UnregisterTimerAsync("MyTimer");
        }

        /// <summary>
        /// Timer callback once timer is expired
        /// </summary>
        private Task OnTimerCallBack(byte[] data)
        {
            Console.WriteLine("OnTimerCallBack is called!");
            return Task.CompletedTask;
        }

        /* Custom Implementations */

        public async Task<bool> VerifyWeighingState(Weighing data)
        {
            await StateManager.SetStateAsync<Weighing>("my_weighings", data);

            var actorData = await StateManager.TryGetStateAsync<Weighing>("my_weighings");

            string jsonData = JsonSerializer.Serialize<Weighing>(actorData.Value);

            await this.RegisterTimerAsync(
                "WeighTimer",
                nameof(this.VerifyWeighingStateRecursive),
                Encoding.UTF8.GetBytes(jsonData),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));

            Console.WriteLine($" ACTOR ID {this.Id} TASK STARTING");
            await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{data.Tenant}/{data.Kiosk}", $"Actor ID: {this.Id} task starting");

            return true;
        }

        public async Task VerifyWeighingStateRecursive([NotNull] byte[] data)
        {
            var jsonData = Encoding.UTF8.GetString(data);
            Weighing weighingData = JsonSerializer.Deserialize<Weighing>(json: jsonData)!;
            Console.WriteLine($"ACTOR DATA: {jsonData}");

            var delay = DateTime.Now - weighingData.Date;
            if (weighingData.State == "VB" && delay.TotalMilliseconds > 15000) 
            {
                string wKey = $"{key}/{weighingData.Tenant}/{weighingData.Kiosk}";
                WeighingRegistry lastWeighing = await _daprClient.GetStateAsync<WeighingRegistry>(storeName, wKey);
                Console.WriteLine($"LASTWEIGHING STATE: {lastWeighing.State}");

                if (weighingData.Date == lastWeighing.Date)
                {
                    lastWeighing.State = "ERROR";
                    await _daprClient.SaveStateAsync(storeName, wKey, lastWeighing);
                    lastWeighing = await _daprClient.GetStateAsync<WeighingRegistry>(storeName, wKey);
                    if (lastWeighing.State.Contains("ERROR")) 
                    { 
                        await this.UnregisterTimerAsync("WeighTimer");
                        Console.WriteLine($"LASTWEIGHING STATE CHANGED: {lastWeighing.State}");
                        Console.WriteLine($"ACTOR ID: {this.Id} TASK COMPLETED");

                        await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{lastWeighing.Tenant}/{lastWeighing.Kiosk}", lastWeighing);
                        await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{lastWeighing.Tenant}/{lastWeighing.Kiosk}", $"Actor ID: {this.Id} task completed, actor finalized");

                        await this.OnDeactivateAsync();
                    }
                }
            } else if (weighingData.State == "AG")
            {
                await this.UnregisterTimerAsync("WeighTimer");
                Console.WriteLine($"ACTOR ID: {this.Id} TASK COMPLETED");

                await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{weighingData.Tenant}/{weighingData.Kiosk}", weighingData);
                await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{weighingData.Tenant}/{weighingData.Kiosk}", $"Actor ID: {this.Id} task completed, actor finalized");

                await this.OnDeactivateAsync();
            } else if (weighingData.State == "VB" && delay.TotalMilliseconds <= 10000)
            {
                await _daprClient.PublishEventAsync("mqtt-pubsub", $"{key}/{weighingData.Tenant}/{weighingData.Kiosk}", weighingData);
            }
        }
    }
}
