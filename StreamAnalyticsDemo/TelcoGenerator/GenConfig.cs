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

        // Constructor
        
        // Assume there is only 1 file, and 1 set
        public GenConfig(float _invalid, float _callback, int hours)
        {
            nSets = 1;
            nFilesPerDump = 1;
            nCDRPerFile = 500;
            nInvalidPercent = _invalid;
            nCallBackPercent = _callback;
            nDurationHours = hours;
        }

        override public String ToString()
        {
            return "#Sets: " + nSets + ",#FilesDump: " + nFilesPerDump + ",#CDRPerFile: " + nCDRPerFile + ",%Invalid: " + nInvalidPercent + ",%CallBack: " + nCallBackPercent + ", #DurationHours: " + nDurationHours;
        }
    }
}
