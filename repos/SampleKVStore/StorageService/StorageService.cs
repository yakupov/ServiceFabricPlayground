using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;
using System.Text.Json;

namespace StorageService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class StorageService : StatefulService, IStorageService
    {
        private readonly string _partitionName;

        private Dictionary<string, string>? store;
        private readonly string _templateFilePath;

        private readonly object _lock = new();

        public StorageService(StatefulServiceContext context)
            : base(context)
        { 
            _partitionName = context.PartitionId.ToString();

            var configPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = configPackage.Settings.Sections["MyConfigSection"];


            _templateFilePath = section.Parameters["StorageFilePath"].Value;
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(store);
            File.WriteAllText(GetStoragePath(), json);
        }

        private void Load()
        {
            string _storagePath = GetStoragePath();

            if (File.Exists(_storagePath))
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Loading store from {0}", _storagePath);
                var json = File.ReadAllText(_storagePath);
                store = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            else
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Not loading store from {0} =(", _storagePath);
                store = [];
            }
        }

        private string GetStoragePath()
        {
            var partitionKey = ((Int64RangePartitionInformation)Partition.PartitionInfo).LowKey;
            var _storagePath = Path.Combine(Path.GetDirectoryName(_templateFilePath)!, $"state_{partitionKey}.json");
            return _storagePath;
        }

        private void CheckLoad()
        {
            if (store == null)
            {
                Load();
            }
        }

        public Task<string> GetAsync(string key)
        {
            lock (_lock)
            {
                CheckLoad();
                string? value = store.TryGetValue(key, out string? retrievedValue) ? retrievedValue : null;
                return Task.FromResult(value ?? "");
            }
        }

        public Task PutAsync(string key, string value)
        {
            lock (_lock)
            {
                CheckLoad();
                //Console.WriteLine(this._partitionName + ": " + key + "=" + value);
                ServiceEventSource.Current.ServiceMessage(this.Context, this._partitionName + ": " + key + "=" + value);
                store[key] = value;
                Save();
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return ServiceRemotingExtensions.CreateServiceRemotingReplicaListeners(this);
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            //var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    //var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Store size on part {0}: {1}", _partitionName, store == null ? "null" : store.Count);

                    //await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}
