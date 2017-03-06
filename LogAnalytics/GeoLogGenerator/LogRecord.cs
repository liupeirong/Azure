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

namespace geoLogGen
{
    [DataContract]
    public class LogRecord
    {
        [DataMember]
        public String eventTime {get; set; }
        
        [DataMember]
        public String playerId {get; set; }
        
        [DataMember]
        public String sessionId {get; set; }

        [DataMember]
        public String eventType {get; set; }

        [DataMember]
        public String eventParams {get; set; }

        static string[] columns = { "eventTime", "playerId", "sessionId", "eventType", "eventParams" };
        
        public LogRecord(Session cur)
        {
            this.eventTime = cur.eventTime.ToString("s");
            this.playerId = cur.playerId.ToString();
            this.sessionId = cur.sessionId.ToString();
            this.eventType = cur.eventType.ToString();
            this.eventParams = cur.eventParams;  
        }

        public String ToCSV()
        {
            return String.Format("{0},{1},{2},{3},{4}\n",
                this.eventTime, this.playerId, this.sessionId, this.eventType, this.eventParams);
        }

        public String ToJSON()
        {
            return String.Format(
                "{{\"eventTime\":\"{0}\",\"playerId\":\"{1}\",\"sessionId\":\"{2}\",\"eventType\":\"{3}\",\"eventParams\":{4}}}\n",
                this.eventTime, this.playerId, this.sessionId, this.eventType, this.eventParams);
            //return JsonConvert.SerializeObject(this);
        }
    }
}
