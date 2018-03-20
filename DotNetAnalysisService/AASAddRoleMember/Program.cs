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
            using (PowerShell ps = PowerShell.Create())
            {
                var username = "admin@contoso.com";
                // put password in a secure store in production, for example Azure Key Vault
                var password = System.IO.File.ReadAllText(@"c:\user\admin\secret.txt");
                SecureString securepwd = new NetworkCredential("", password).SecurePassword;
                PSCredential cred = new PSCredential(username, securepwd);

                var aasServer = "asazure://westeurope.asazure.windows.net/contosoaas";
                var aasDB = "contosoModel";
                var role2AddTo = "contosoRead";
                var user2Add = "memeber1@contoso.com";

                // Ensre sqlserver module is installed, install-module sqlserver
                var PSOutput = ps
                    .AddScript("import-module sqlserver")
                    .AddStatement()
                    .AddCommand("add-rolemember")
                    .AddParameter("server", aasServer)
                    .AddParameter("database", aasDB)
                    .AddParameter("RoleName", role2AddTo)
                    .AddParameter("MemberName", user2Add)
                    .AddParameter("Credential", cred)
                    .Invoke();

                if (ps.Streams.Error.Count > 0)
                {
                    Console.WriteLine(ps.Streams.Error[0].ErrorDetails.Message);
                }
            }
        }
    }
}
