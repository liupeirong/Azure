using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.Entity;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using System.Configuration;

namespace SQLSyncTest
{
    class SyncMonitor
    {
        //figure out who are the leaders and who are the followers
        //spin a thread to update the leaders periodically
        //spin a thread to check the followers periodically
        //if status changes, dump status to azure blob

        [DataContract]
        public class Follower
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public int Value { get; set; }
            public DateTime LastUpdated { get; set; }
            [DataMember(Name="LastUpdatedAt")]
            public string LastUpdatedStr { get; set; }
            public TimeSpan SyncDelayed { get; set; }
            [DataMember(Name="DelayedBy")]
            public string SyncDelayedStr { get; set; }
            [DataMember]
            public string Status { get; set; }
        };
        [DataContract]
        public class Leader
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public int Value { get; set; }
            public DateTime LastUpdated { get; set; }
            [DataMember(Name = "LastUpdatedAt")]
            public string LastUpdatedStr { get; set; }
            [DataMember(Name="children")]
            public List<Follower> Followers { get; set; }
        };

        private List<Leader> leaders;
        private Dictionary<string, string> dbCxnMap;
        private int maxDelayinSeconds;
        private int checkinSeconds;
        private readonly string[] StatusCode = { "up-to-date", "behind", "out-of-date" };
        private CloudBlobContainer container;
        private readonly string blobName = "syncstatus";
        private System.Threading.Timer queryTimer;
        private System.Threading.Timer updateTimer;

        public SyncMonitor() 
        {
        }

        public void Initialize()
        {
            maxDelayinSeconds = int.Parse(CloudConfigurationManager.GetSetting("syncmaxdelayinseconds"));
            checkinSeconds = int.Parse(CloudConfigurationManager.GetSetting("synccheckinseconds"));
            //get all the dbs and their connectionstrings
            char[] separators = {';'};
            dbCxnMap = new Dictionary<string,string>();
            string []clients = CloudConfigurationManager.GetSetting("syncgroup").Split(separators);
            foreach (string client in clients)
            {
                dbCxnMap[client] = ConfigurationManager.ConnectionStrings[client].ConnectionString;
            }

            //find the leaders and followers
            leaders = new List<Leader>();
            foreach (string client in clients)
            {
                Leader ld = new Leader();
                ld.Name = client;
                ld.Followers = new List<Follower>();
                ld.LastUpdated = DateTime.MinValue;
                foreach (string others in clients)
                {
                    if (String.Compare(others, client) == 0) continue;
                    Follower fl = new Follower();
                    fl.Name = others;
                    ld.Followers.Add(fl);
                }
                leaders.Add(ld);
            }

            //the blob that contains the status info
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("blobConnectionString"));
            string blobContainer = CloudConfigurationManager.GetSetting("blobContainer");
            //create blob container and set permission
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(blobContainer);
            container.CreateIfNotExists();
            BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            container.SetPermissions(containerPermissions);
        }

        private void QueryState(Object state)
        {
            //get the Sync table from each DB
            Dictionary<string, Dictionary<string, SyncAgent>> syncTables = new Dictionary<string,Dictionary<string, SyncAgent>>();
            foreach (string key in dbCxnMap.Keys)
            {
                try
                {
                    using (SyncMonDB db = new SyncMonDB(dbCxnMap[key]))
                    {
                        Dictionary<string, SyncAgent> rows = new Dictionary<string, SyncAgent>();
                        IEnumerable<SyncAgent> query = (from r in db.SyncAgents select r);
                        foreach (SyncAgent row in query)
                        {
                            rows[row.Name] = row;
                        }
                        syncTables[key] = rows;
                    }
                } catch (Exception e)
                {
                    Trace.TraceError("failed to access db {0}: {1}", key, e.Message);
                    syncTables[key] = null;
                }
            }
            //update the state in memory
            foreach (Leader leader in leaders)
            {
                // find leader's db
                Dictionary<string, SyncAgent> rows = syncTables[leader.Name];
                if (rows == null) continue; //can't connect to leader's db
                // find leader's value in leader's db
                leader.Value = rows[leader.Name].Value;
                leader.LastUpdated = rows[leader.Name].LastUpdated;
                leader.LastUpdatedStr = leader.LastUpdated.ToString("g");
                foreach(Follower follower in leader.Followers)
                {
                    // find follower's db
                    Dictionary<string, SyncAgent> flrows = syncTables[follower.Name];
                    if (flrows == null) continue; //can't connect to follower's db
                    follower.Status = StatusCode[2]; //default is out-of-date
                    if (!flrows.ContainsKey(leader.Name)) continue; //follower doesn't have leader info yet
                    // find leader's value in follower's db
                    follower.Value = flrows[leader.Name].Value;
                    follower.LastUpdated = flrows[leader.Name].LastUpdated;
                    follower.LastUpdatedStr = follower.LastUpdated.ToString("g");
                    follower.SyncDelayed = leader.LastUpdated - follower.LastUpdated;
                    follower.SyncDelayedStr = follower.SyncDelayed.ToString(@"dd\.hh\:mm\:ss") + " days";
                    if (follower.Value == leader.Value)
                        follower.Status = StatusCode[0];
                    else if (follower.SyncDelayed.TotalSeconds < maxDelayinSeconds)
                        follower.Status = StatusCode[1];
                }
            };

            DumpState();
        }

        public void UpdateLeaders(Object state)
        {
            foreach (Leader leader in leaders)
            {
                TimeSpan elapsed = DateTime.Now - leader.LastUpdated; 
                if (elapsed.TotalSeconds < maxDelayinSeconds) continue;
                // update leader value in the leader db
                try
                {
                    using (SyncMonDB db = new SyncMonDB(dbCxnMap[leader.Name]))
                    {
                        //if the leader value doesn't exist, insert, otherwise, update
                        SyncAgent row = (from r in db.SyncAgents where r.Name == leader.Name select r).SingleOrDefault();
                        if (row == null)
                        {
                            row = new SyncAgent();
                            row.Name = leader.Name;
                            row.Value = (new Random()).Next(0, 1000);
                            row.LastUpdated = DateTime.Now;
                            db.SyncAgents.Add(row);
                        }
                        else
                        {
                            row.Value = (new Random()).Next(0, 1000);
                            row.LastUpdated = DateTime.Now;
                        }
                        db.SaveChanges();
                    }
                } catch (Exception e)
                {
                    Trace.TraceError("Failed to update leader {0}. {1}", leader.Name, e.Message);
                }
            }
        }

        public void DumpState()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Leader>));
            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, leaders);
                ms.Position = 0;
                //for local debugging
                //using (FileStream fs = new FileStream("c:\\temp\\syncstate.txt", FileMode.Create, FileAccess.Write))
                //{
                //    ms.CopyTo(fs);
                //}
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                blob.UploadFromStream(ms);
            };
        }

        public void Run()
        {
            Trace.TraceInformation("Data sync monitor job started.");
            Initialize();
            Database.SetInitializer<SyncMonDB>(null);
            ManualResetEvent forever = new ManualResetEvent(false);

            updateTimer = new System.Threading.Timer(UpdateLeaders, null, 1000, maxDelayinSeconds * 1000);
            queryTimer = new System.Threading.Timer(QueryState, null, 5000, checkinSeconds * 1000);

            forever.WaitOne();
        }
    }
}
