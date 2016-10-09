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

namespace telcodatagen
{

    class GenConfig
    {
        public int nSets { get; set; }
        public int nFilesPerDump { get; set; }
        public int nCDRPerFile { get; set; }
        public float nCallBackPercent { get; set; }
        public float nInvalidPercent { get; set; }
        public int nDurationHours { get; set; }
        public bool isPowerBiPro { get; set; }
        public bool isOutputJson { get; set; }
        public bool isPrintOnly { get; set; }

        // Constructor

        // Assume there is only 1 file, and 1 set
        public GenConfig(string[] args)
        {
            int cArgs = args.Length;
            float invalid = cArgs > 0 ? float.Parse(args[0]) : (float)0.02;
            float callback = cArgs > 1 ? float.Parse(args[1]) : (float)0.05;
            int hours = cArgs > 2 ? int.Parse(args[2]) : 1;
            int powerbiPro = cArgs > 3? int.Parse(args[3]) : 0;
            int outputJson = cArgs > 4? int.Parse(args[4]) : 1;
            int printOnly = cArgs > 5 ? int.Parse(args[5]) : 0;

            nSets = 1;
            nFilesPerDump = 1;
            nCDRPerFile = 500;
            nInvalidPercent = invalid;
            nCallBackPercent = callback;
            nDurationHours = hours;
            isPowerBiPro = (powerbiPro == 1);
            isOutputJson = (outputJson == 1);
            isPrintOnly = (printOnly == 1);
        }

        override public String ToString()
        {
            return "#Sets: " + nSets + ",#FilesDump: " + nFilesPerDump + ",#CDRPerFile: " + nCDRPerFile + ",%Invalid: " + nInvalidPercent + ",%CallBack: " + nCallBackPercent + ", #DurationHours: " + nDurationHours;
        }
    }
}
