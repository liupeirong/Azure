using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.DataLake.Store;
using System.Configuration;

namespace eh2adlsJob
{
    class EventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;
        TextWriter log;
        string partitionID;

        DataLakeStoreAccountManagementClient adlsClient;
        DataLakeStoreFileSystemManagementClient adlsFileSystemClient;
        string adlsAccountName;
        string remoteFilePath;

        public EventProcessor(PartitionContext context, TextWriter logger, int numCreated)
        {
            log = logger;
            partitionID = context.Lease.PartitionId;
        }

        private void InitializeADLS()
        {
            this.adlsAccountName = ConfigurationManager.AppSettings["adlsAccountName"];
            string subId = ConfigurationManager.AppSettings["subId"];
            string remoteFolderPath = "/Hackfest/";
            remoteFilePath = remoteFolderPath + "test" + partitionID + ".txt";

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            var domain = ConfigurationManager.AppSettings["domain"];
            var webApp_clientId = ConfigurationManager.AppSettings["webApp_clientId"];
            var clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            var clientCredential = new ClientCredential(webApp_clientId, clientSecret);
            var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

            adlsClient = new DataLakeStoreAccountManagementClient(creds);
            adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);
            adlsClient.SubscriptionId = subId;

            //create directory if not already exist
            if (!adlsFileSystemClient.FileSystem.PathExists(adlsAccountName, remoteFolderPath))
            {
                adlsFileSystemClient.FileSystem.Mkdirs(adlsAccountName, remoteFolderPath);
            }
            //create/overwrite the file
            string header = "";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(header));
            adlsFileSystemClient.FileSystem.Create(adlsAccountName, remoteFilePath, stream, true);
        }

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            log.WriteLine("Processor Shutting Down. Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason);
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            log.WriteLine("SimpleEventProcessor initialized.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);

            InitializeADLS();
            
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            string content = "";
            foreach (EventData eventData in messages)
            {
                content += Encoding.UTF8.GetString(eventData.GetBytes());
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            adlsFileSystemClient.FileSystem.Append(adlsAccountName, remoteFilePath, stream);

            //Call checkpoint every 5 minutes, so that worker can resume processing from 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }
    }

    class EventProcessorFactory : IEventProcessorFactory
    {
        private TextWriter logger;
        private int numCreated = 0;
        public EventProcessorFactory(TextWriter log)
        {
            this.logger = log;
        }

        IEventProcessor IEventProcessorFactory.CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(context, logger, Interlocked.Increment(ref numCreated));
        }
    }
}
