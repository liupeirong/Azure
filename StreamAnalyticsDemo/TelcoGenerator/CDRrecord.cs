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
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace telcodatagen
{
    
    [DataContract]
    public class CDRrecord
    {
        [DataMember]
        public String RecordType {get; set; }
        
        [DataMember]
        public String SystemIdentity {get; set; }
        
        [DataMember]
        public String SwitchNum {get; set; }

        [DataMember]
        public String CallingNum {get; set; }

        [DataMember]
        public String CallingIMSI {get; set; }

        [DataMember]
        public String CalledNum {get; set; }

        [DataMember]
        public String CalledIMSI {get; set; }

        [DataMember]
        public int TimeType {get; set; }

        [DataMember]
        public  int CallPeriod {get; set; }

        [DataMember]
        public double UnitPrice { get; set; }

        [DataMember]
        public String ServiceType {get; set; }

        [DataMember]
        public int Transfer {get; set; }

        [DataMember]
        public String EndType { get; set; }

        [DataMember]
        public String IncomingTrunk {get; set; }

        [DataMember]
        public String OutgoingTrunk {get; set; }

        [DataMember]
        public String MSRN {get; set; }

        [DataMember]
        public String callrecTime {get; set; }


        static string[] columns = {"RecordType","SystemIdentity","SwitchNum","CallingNum","CallingIMSI",
            "CalledNum","CalledIMSI","TimeType","CallPeriod","UnitPrice","ServiceType",
            "Transfer","EndType","IncomingTrunk","OutgoingTrunk","MSRN","DateTime"};

        static string[] ServiceTypeList = { "a", "b", "S", "V" };
        static string[] TimeTypeList = { "a", "d", "r", "s" };
        static string[] EndTypeList = { "0", "3", "4"};
        static string[] OutgoingTrunkList = { "F", "442", "623", "418", "425", "443", "426", "621", "614", "609", "419", "402", "411", "422", "420", "423", "421", "300", "400", "405", "409", "424" };
        static string[] IMSIList = { "466923300507919","466921602131264","466923200348594","466922002560205","466922201102759","466922702346260","466920400352400","466922202546859","466923000886460","466921302209862","466923101048691","466921200135361","466922202613463","466921402416657","466921402237651","466922202679249","466923300236137","466921602343040","466920403025604","262021390056324","466920401237309","466922000696024","466923100098619","466922702341485","466922200432822","466923000464324","466923200779222","466923100807296","466923200408045" };
        static string[] MSRNList = { "886932428687", "886932429021", "886932428306", "1415982715962", "886932429979", "1416916990491", "886937415371", "886932428876", "886932428688", "1412983121877", "886932429242", "1416955584542", "886932428258", "1412930064972", "886932429155", "886932423548", "1415980332015", "14290800303585", "14290800033338", "886932429626", "886932428112", "1417955696232", "1418986850453", "886932428927", "886932429827", "886932429507", "1416960750071", "886932428242", "886932428134", "886932429825" ,""};
        
        static Random coin = new Random();

        public CDRrecord()
        {
            _init();
        }

        public void _init()
        {
            int idx = 0;
            // Initialize default values
            this.SystemIdentity = "d0";
            this.RecordType = "MO";

            idx = coin.Next(0, TimeTypeList.Length);
            this.TimeType = idx;

            idx = coin.Next(0, ServiceTypeList.Length);
            this.ServiceType = ServiceTypeList[idx];

            idx = coin.Next(0, EndTypeList.Length);
            this.EndType = EndTypeList[idx];

            idx = coin.Next(0, OutgoingTrunkList.Length);
            this.OutgoingTrunk = OutgoingTrunkList[idx];

            this.Transfer = coin.Next(0, 2);

            idx = coin.Next(0, IMSIList.Length);
            this.CallingIMSI = IMSIList[idx];

            idx = coin.Next(0, IMSIList.Length);
            this.CalledIMSI = IMSIList[idx];

            idx = coin.Next(0, MSRNList.Length);
            this.MSRN = MSRNList[idx];
        }

        // set the data for the CDR record
        public void setData(string key, string value)
        {
            switch (key)
            {
                case "RecordType": 
                    this.RecordType = value;
                    break;
                case "SystemIdentity":
                    this.SystemIdentity = value;
                    break;
                case "SwitchNum":
                    this.SwitchNum = value;
                    break;
                case "CallingNum":
                    this.CallingNum = value;
                    break;
                case "CallingIMSI":
                    this.CallingIMSI = value;
                    break;
                case "CalledNum":
                    this.CalledNum = value;
                    break;
                case "CalledIMSI":
                    this.CalledIMSI = value;
                    break;
                case "TimeType":
                    this.TimeType = Int32.Parse(value);
                    break;
                case "CallPeriod":
                    this.CallPeriod = Int32.Parse(value);
                    break;
                case "UnitPrice":
                    this.UnitPrice = Double.Parse(value);
                    break;
                case "ServiceType":
                    this.ServiceType = value;
                    break;
                case "Transfer":
                    this.Transfer = Int32.Parse(value);
                    break;                
                case "IncomingTrunk":
                    this.IncomingTrunk = value;
                    break;
                case "OutgoingTrunk":
                    this.OutgoingTrunk = value;
                    break;
                case "MSRN":
                    this.MSRN = value;
                    break;
                case "DateTime":
                    if (value.Length > 13) { 
                        int hour = Int32.Parse(value.Substring(9, 2));
                        int min = Int32.Parse(value.Substring(11, 2));
                        int secs = Int32.Parse(value.Substring(13, 2));

                        int year = Int32.Parse(value.Substring(0, 4));
                        int month = Int32.Parse(value.Substring(4, 2));
                        int day = Int32.Parse(value.Substring(6, 2));

                        this.callrecTime = new DateTime(year, month, day, hour, min, secs).ToUniversalTime().ToString(@"yyyy-MM-ddThh:mm:ssZ");
                    }
                    
                    break;
            }
        }

        public String ToCSV()
        {
            return String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16}",
                this.RecordType, this.SystemIdentity, this.SwitchNum, this.CallingNum, this.CallingIMSI,
                this.CalledNum, this.CalledIMSI, this.TimeType, this.CallPeriod, this.UnitPrice, this.ServiceType,
                this.Transfer, this.EndType, this.IncomingTrunk, this.OutgoingTrunk, this.MSRN, this.callrecTime);
        }

        public String ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
