﻿//*********************************************************
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


namespace telcodatagen
{
    class Program
    {
        static string eventHubName;
        static EventHubClient client;
        static TextWriter writer = null;

        static void Main(string[] args)
        {
            // Show Usage information
            if (args.Length < 3)
                Usage();

            // Setup service bus
            string connectionString = GetServiceBusConnectionString();
            NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            eventHubName = ConfigurationManager.AppSettings["EventHubName"];
            client = EventHubClient.Create(eventHubName);

            GenerateData(args);
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

        static void GenerateData(string[] args)
        {
            Queue callBackQ = new Queue();

            // probability of invalid calls, probability of fraud, and number of hours
            GenConfig config = new GenConfig(float.Parse(args[0]), float.Parse(args[1]), Int32.Parse(args[2]));
            Console.Error.WriteLine(config);

            CallStore mobileNos = new CallStore(100000);
            Random r = new Random();
            bool invalidRec = false;
            bool genCallback = false;
            DateTimeOffset currentTime = DateTimeOffset.Now;
            DateTimeOffset endTime = currentTime.AddHours(config.nDurationHours);
            // power bi free subscription won't allow more than 10,000 records per hour per dataset, so roughly 3 records per hour
            // we can generate a cdr per second, plus a fraud cdr, plus aggregation record
            int milliSecPerCDR = 1000;
            int fraudConsecutiveCallInSec = 5;

            while (endTime.Subtract(currentTime) >= TimeSpan.Zero)
            {
                for (int cdr = 0; cdr < config.nCDRPerFile; cdr++)
                {
                    #region initRecord
                    // Determine whether to generate an invalid CDR record
                    double pvalue = r.NextDouble();
                    invalidRec = (pvalue < config.nInvalidPercent);

                    // Determine whether there will be a callback
                    pvalue = r.NextDouble();
                    genCallback = (pvalue < config.nCallBackPercent);
                    
                    // Determine called and calling num
                    int calledIdx = r.Next(0, mobileNos.CallNos.Length);
                    int callingIdx = r.Next(0, mobileNos.CallNos.Length);
                    
                    CDRrecord rec = new CDRrecord();

                    int switchIdx = r.Next(0, mobileNos.switchCountries.Length);
                    int switchAltIdx = r.Next(0, mobileNos.switchCountries.Length);
                    while (switchAltIdx == switchIdx)
                        switchAltIdx = r.Next(0, mobileNos.switchCountries.Length);
                    rec.setData("SwitchNum", mobileNos.switchCountries[switchIdx]);
                    rec.setData("UnitPrice", "" + mobileNos.dollarPerMin[switchIdx]);
                    #endregion

                    if (invalidRec)
                    {
                        rec.setData("DateTime", "F F");
                    }
                    else
                    {
                        String callDate = String.Format("{0:yyyyMMdd}", currentTime);
                        String callTime = String.Format("{0:HHmmss}", currentTime);
                        rec.setData("DateTime", callDate + " " + callTime);

                        String calledNum = mobileNos.CallNos[calledIdx];
                        String callingNum = mobileNos.CallNos[callingIdx];
                        rec.setData("CalledNum", calledNum);
                        rec.setData("CallingNum", callingNum);
                        int callPeriod = r.Next(1, 80);
                        rec.setData("CallPeriod", "" + callPeriod);

                        #region simFraudRecord
                        if (genCallback)
                        {
                            // need to generate another set of no
                            calledIdx = r.Next(0, mobileNos.CallNos.Length); ;
                            callingIdx = r.Next(0, mobileNos.CallNos.Length);

                            CDRrecord callbackRec = new CDRrecord();
                            callbackRec.setData("SwitchNum", mobileNos.switchCountries[switchAltIdx]);
                            callbackRec.setData("UnitPrice", "" + mobileNos.dollarPerMin[switchAltIdx]);

                            // Pertub second 
                            int pertubs = r.Next(0, fraudConsecutiveCallInSec);
                            callDate = String.Format("{0:yyyyMMdd}", currentTime);
                            callTime = String.Format("{0:HHmmss}", currentTime.AddSeconds(pertubs));
                            callbackRec.setData("DateTime", callDate + " " + callTime);

                            // Set it as the same calling IMSI
                            callbackRec.setData("CallingIMSI", rec.CallingIMSI);

                            calledNum = mobileNos.CallNos[calledIdx];
                            callingNum = mobileNos.CallNos[callingIdx];
                            callbackRec.setData("CalledNum", calledNum);
                            callbackRec.setData("CallingNum", callingNum);

                            callPeriod = r.Next(1, 100);
                            callbackRec.setData("CallPeriod", "" + callPeriod);

                            // Enqueue the call back rec 
                            callBackQ.Enqueue(callbackRec);
                            cdr++;
                        }
                        #endregion
                    }

                    outputCDRRecs(rec);
                    //get those consecutive fraud calls after the interval passed
                    while (callBackQ.Count > 0 && cdr % (fraudConsecutiveCallInSec+1) == 0) 
                    {
                        CDRrecord drec;
                        drec = (CDRrecord)callBackQ.Dequeue();
                        outputCDRRecs(drec);
                    }

                    System.Threading.Thread.Sleep(milliSecPerCDR);
                    currentTime = DateTimeOffset.Now;
                } // cdr

                #region close the file
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                    writer = null;
                }
                #endregion
                
                System.Threading.Thread.Sleep(milliSecPerCDR);
                currentTime = DateTimeOffset.Now;
            } // while - within duration
        }

        // Print usage information
        static void Usage()
        {
            // In this case, we treat the #FilesPerDump as the number of switch, which is not 100% true

            Console.WriteLine("Usage: telcodatagen [#NumCDRsPerHour] [SIM Card Fraud Probability] [#DurationHours]");
            System.Environment.Exit(-1);
        }


        // Handle output of cdr recs
        static void outputCDRRecs(CDRrecord r)
        {
            try
            {
                List<Task> tasks = new List<Task>();
                var serializedString = JsonConvert.SerializeObject(r);
                EventData data = new EventData(Encoding.UTF8.GetBytes(serializedString))
                {
                    PartitionKey = r.CallingIMSI
                };

                // Send the metric to Event Hub
                tasks.Add(client.SendAsync(data));
                Console.WriteLine("CDRrecord:" + r);

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on send: " + e.Message);
            }
        }

    }
}
