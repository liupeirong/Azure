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
            string source = ConfigurationManager.AppSettings["source"];
            switch (source)
            {
                case "eventhub":
                    log.WriteLine("process EventHub.");
                    ProcessEventHub(log);
                    break;
                case "servicebusqueue":
                    log.WriteLine("process ServiceBus Queue.");
                    ProcessServiceBusQueue(log);
                    break;
                default: 
                    log.WriteLine("No sink specified.");
                    break;
            }
        }

        private static void ProcessEventHub(TextWriter log)
        { 
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
            options.ExceptionReceived += (sender, e) => ProcessEventHubException(sender, e, log, oSignalProcessEnd);
            eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(log), options).Wait();
            oSignalProcessEnd.WaitOne();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }

        private static void ProcessEventHubException(object sender, ExceptionReceivedEventArgs e, TextWriter log, ManualResetEvent oSignalProcessEnd)
        {
            log.WriteLine(e.Exception);
            oSignalProcessEnd.Set();
        }

        private static void ProcessServiceBusQueue(TextWriter log)
        {
            log.WriteLine("process servicebus");
        }
    }
}
