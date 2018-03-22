using System;
using System.Management.Automation;
using System.Net;
using System.Security;

namespace PowerShellExecutionSample
{
    class Program
    {
        static void Main(string[] args)
        {
            PowerShellExecutor t = new PowerShellExecutor();
            t.ExecuteSynchronously();
        }
    }

    class PowerShellExecutor
    {
        public void ExecuteSynchronously()
        {
            bool useServicePrincipal = false;
            using (PowerShell ps = PowerShell.Create())
            {
                var aasEnv = "contosoregion.asazure.windows.net";
                var aasServer = "contosoaas";
                var aasDB = "contosodb";
                var role = "contosoRead";
                var user2Add = "joe@contoso.com";
                var tenantId = "contoso azure ad tenant id"; //only needed for service principal
                var adminuser = useServicePrincipal ? 
                    "contoso service principal" :
                    "admin@contoso.com";
                // put password in a secure store in production, for example Azure Key Vault
                var adminpwd = System.IO.File.ReadAllText(@"c:\users\admin\secret.txt");

                var aasURL = "asazure://" + aasEnv + "/" + aasServer;
                SecureString securepwd = new NetworkCredential("", adminpwd).SecurePassword;
                PSCredential cred = new PSCredential(adminuser, securepwd);

                if (useServicePrincipal)
                {
                    ps.AddScript("Import-Module Azure.AnalysisServices")
                        .AddStatement()
                        .AddCommand("Add-AzureAnalysisServicesAccount")
                        .AddParameter("ServicePrincipal")
                        .AddParameter("TenantId", tenantId)
                        .AddParameter("RolloutEnvironment", aasEnv)
                        .AddParameter("Credential", cred)
                        .AddStatement()
                        .AddScript("Import-Module SqlServer")
                        .AddStatement()
                        .AddCommand("Add-RoleMember")
                        .AddParameter("Server", aasURL)
                        .AddParameter("Database", aasDB)
                        .AddParameter("RoleName", role)
                        .AddParameter("MemberName", user2Add)
                        .Invoke();
                }
                else
                {
                    ps.AddScript("Import-Module SqlServer")
                        .AddStatement()
                        .AddCommand("Add-RoleMember")
                        .AddParameter("Server", aasURL)
                        .AddParameter("Database", aasDB)
                        .AddParameter("RoleName", role)
                        .AddParameter("MemberName", user2Add)
                        .AddParameter("Credential", cred)
                        .Invoke();
                }

                if (ps.Streams.Error.Count > 0)
                {
                    Console.WriteLine(ps.Streams.Error[0].Exception.InnerException.Message);
                }
            }
        }
    }
}
