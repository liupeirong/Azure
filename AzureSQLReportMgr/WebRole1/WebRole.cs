using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;

namespace ReportWebSite
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            //if we don't do this, Azure tracelistener only runs in the cloud, and we can't run this app standalone.
            Dictionary<string, SourceLevels> traceLevelMap = new Dictionary<string, SourceLevels>()
            {
                {"All", SourceLevels.All},
                {"Critical", SourceLevels.Critical},
                {"Error", SourceLevels.Error},
                {"Warning", SourceLevels.Warning},
                {"Information", SourceLevels.Information},
                {"ActivityTracing", SourceLevels.ActivityTracing},
                {"Verbose", SourceLevels.Verbose},
                {"Off", SourceLevels.Off}
            };
            if (RoleEnvironment.IsAvailable)
            {
                string cloudTraceLevel = CloudConfigurationManager.GetSetting("cloudTraceLevel");
                SourceLevels sl = traceLevelMap.ContainsKey(cloudTraceLevel) ? traceLevelMap[cloudTraceLevel] : SourceLevels.Error;

                Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener tr =
                    new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener();
                tr.Filter = new EventTypeFilter(sl);

                Trace.Listeners.Add(tr);
                Trace.AutoFlush = true;
            }

            return base.OnStart();
        }
    }
}
