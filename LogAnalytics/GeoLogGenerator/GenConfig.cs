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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace geoLogGen
{
    class GenConfig
    {
        public int msPerSession { get; set; }
        public int numPlayers { get; set; }
        public int nDurationHours { get; set; }
        public bool isPrintOnly { get; set; }
        public string cityFile { get; set; }

        // Assume there is only 1 file, and 1 set
        public GenConfig(string[] args)
        {
            try
            {
                int cArgs = args.Length;
                nDurationHours = cArgs > 0 ? int.Parse(args[0]) : 1;
                numPlayers = cArgs > 1 ? int.Parse(args[1]) : 200;
                bool isPowerBiPro = (cArgs > 2) && (args[2] == "--pro");
                isPrintOnly = (cArgs > 3) && (args[3] == "--print-only");
                cityFile = cArgs > 4 ? args[4] : ".\\world_cities.csv";

                // how many sessions can we generate per second, assuming ~3 log records per session
                int sessionPerSec = isPowerBiPro ? 100 : 1;
                // how much time to pause before generating another session
                msPerSession = (int)(1000 / sessionPerSec);
                Console.WriteLine(String.Format("duration:{0}hours, players:{1}, isPro:{2}, printOnly:{3}, citycsv:{4}",
                    nDurationHours, numPlayers, isPowerBiPro ? "true" : "false", isPrintOnly ? "true" : "false", cityFile));
            }
            catch
            {
                Console.WriteLine("Usage: runDurationHours numPlayers [--pro | --free] [--print-only | --eventhub] [path to city csv]\n");
                System.Environment.Exit(-1);
            }
        }
    }
}
