//*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace geoLogGen
{
    class Program
    {
        static string eventHubName;
        static EventHubClient client;

        static void Main(string[] args)
        {
            GenConfig config = new GenConfig(args);

            // Setup service bus
            string connectionString = GetServiceBusConnectionString();
            NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            eventHubName = ConfigurationManager.AppSettings["EventHubName"];
            client = EventHubClient.Create(eventHubName);

            GenerateData(config);
            Console.ReadKey();
        }

        private static string GetServiceBusConnectionString()
        {
            string connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Did not find Service Bus connections string in appsettings (app.config)");
                return string.Empty;
            }
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);
            builder.TransportType = TransportType.Amqp;
            return builder.ToString();
        }

        static void GenerateData(GenConfig config)
        {
            Queue<LogRecord> futureLogQ = new Queue<LogRecord>();
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            DateTimeOffset endTime = currentTime.AddHours(config.nDurationHours);
            int totalSession = 0;
            Random coin = new Random();
            PlayerPool players = new PlayerPool(config.numPlayers);
            LocationPool locations = new LocationPool(config.cityFile);

            while (endTime.Subtract(currentTime) >= TimeSpan.Zero)
            {
                Session curSession = new Session(coin, players, locations);

                //start
                curSession.setEvent(Session.eventTypeList.gameStart, currentTime);
                LogRecord rec = new LogRecord(curSession); 
                outputLog(rec, config);

                //reachLevel
                int seconds2Level = coin.Next(5, 20);
                curSession.setEvent(Session.eventTypeList.levelReached, currentTime.AddSeconds(seconds2Level));
                rec = new LogRecord(curSession);
                futureLogQ.Enqueue(rec);

                //purchase
                int seconds2Purchase = coin.Next(2, 10);
                curSession.setEvent(Session.eventTypeList.itemPurchased, currentTime.AddSeconds(seconds2Purchase));
                rec = new LogRecord(curSession);
                futureLogQ.Enqueue(rec);

                //end
                int seconds2End = Math.Max(seconds2Level, seconds2Purchase) + coin.Next(1, 5);
                curSession.setEvent(Session.eventTypeList.gameEnd, currentTime.AddSeconds(seconds2End));
                rec = new LogRecord(curSession);
                futureLogQ.Enqueue(rec);

                //output those session end records that happened in the past not future
                while (futureLogQ.Count > 0) 
                {
                    LogRecord drec, prec;
                    prec = (LogRecord)futureLogQ.Peek();
                    DateTimeOffset oldest = DateTimeOffset.Parse(prec.eventTime, null, System.Globalization.DateTimeStyles.AssumeUniversal);
                    if (currentTime.Subtract(oldest) >= TimeSpan.Zero)
                    {
                        drec = (LogRecord)futureLogQ.Dequeue();
                        outputLog(drec, config);
                    }
                    else break;
                }

                System.Threading.Thread.Sleep(config.msPerSession);
                Console.WriteLine("Total:" + totalSession++ + ", In queue:" + futureLogQ.Count);
                currentTime = DateTimeOffset.UtcNow;
            } // while - within duration
        }


        static void outputLog(LogRecord r, GenConfig config)
        {
            try
            {
                var serializedString = r.ToJSON();
                Console.WriteLine("LogRecord:" + serializedString);

                if (!config.isPrintOnly)
                {
                    EventData data = new EventData(Encoding.UTF8.GetBytes(serializedString))
                    {
                        PartitionKey = r.sessionId
                    };

                    client.SendAsync(data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on send: " + e.Message);
            }
        }

    }
}
