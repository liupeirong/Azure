using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using System.Threading;
using System.Configuration;

namespace eh2adlsJob
{
    public class Functions
    {
        [NoAutomaticTriggerAttribute]
        public static void ProcessMessage(TextWriter log)
        {
            log.WriteLine("Enter ProcessMessage.");
            string eventHubConnectionString = ConfigurationManager.AppSettings["eventHubConnectionString"];
            string eventHubName = ConfigurationManager.AppSettings["eventHubName"];
            string consumerGroup = ConfigurationManager.AppSettings["consumerGroup"];
            string storageAccountName = ConfigurationManager.AppSettings["storageAccountName"];
            string storageAccountKey = ConfigurationManager.AppSettings["storageAccountKey"];
            bool newEventsOnly = ConfigurationManager.AppSettings["newEventsOnly"] == "true";
            string storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", 
                storageAccountName, storageAccountKey);
            ManualResetEvent oSignalProcessEnd = new ManualResetEvent(false);

            string eventProcessorHostName = Guid.NewGuid().ToString();
            EventProcessorHost eventProcessorHost = newEventsOnly ? 
                new EventProcessorHost(eventProcessorHostName, eventHubName, consumerGroup , 
                eventHubConnectionString, storageConnectionString, eventProcessorHostName) :
                new EventProcessorHost(eventProcessorHostName, eventHubName, consumerGroup , 
                eventHubConnectionString, storageConnectionString);
            log.WriteLine("Registering EventProcessor...");
            var options = newEventsOnly ?
                new EventProcessorOptions { InitialOffsetProvider = (partitionId) => DateTime.UtcNow } :
                new EventProcessorOptions();
            options.ExceptionReceived += (sender, e) => ProcessException(sender, e, log, oSignalProcessEnd);
            eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(log), options).Wait();
            oSignalProcessEnd.WaitOne();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }

        private static void ProcessException(object sender, ExceptionReceivedEventArgs e, TextWriter log, ManualResetEvent oSignalProcessEnd)
        {
            log.WriteLine(e.Exception);
            oSignalProcessEnd.Set();
        }
    }
}
