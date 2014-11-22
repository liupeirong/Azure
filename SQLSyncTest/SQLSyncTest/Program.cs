using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Data;

namespace SQLSyncTest
{
    class Program
    {
        static void Main(string[] args)
        {
            SyncMonitor sm = new SyncMonitor();
            sm.Run();
        }
    }
}
