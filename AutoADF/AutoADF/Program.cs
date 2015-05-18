using System;
using System.Threading;
using System.Configuration;
using System.Collections.ObjectModel;

using Microsoft.Azure.Management.DataFactories;
using Microsoft.Azure.Management.DataFactories.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace AutoADF
{
    class Program
    {
        const string resourceGroupName = "autoadfrg3";
        const string dataFactoryName = "test37";

        static void Main(string[] args)
        {
            string subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID", EnvironmentVariableTarget.User);
            if (subscriptionId == null)
            {
                Console.WriteLine("Please set SUBSCRIPTION_ID, ACTIVEDIRECTORY_TENANT_ID, SERVICEACCOUNT_USERNAME, SERVICEACCOUNT_PASSWORD in user environment variables");
                Environment.Exit(1);
            }
            string resourceManagementEndpoint = ConfigurationManager.AppSettings["ResourceManagementEndpoint"];
            var header = GetAuthorizationHeader();

            var task = CallAzureResourceManagerApi(resourceManagementEndpoint, subscriptionId, header, resourceGroupName);
            task.Wait();

            //CallSDK(resourceManagementEndpoint, subscriptionId, header, resourceGroupName);
        }

        private static async Task CallAzureResourceManagerApi(string resourceManagementEndpoint, string subscriptionId, string header, string resourceGroupName)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", header);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                //register the subscription with DataFactory provider
                StringContent body = new StringContent("", Encoding.UTF8, "application/json");
                string endpoint = "{0}subscriptions/{1}/providers/Microsoft.DataFactory/register?api-version=2015-01-01";
                string uri = String.Format(endpoint, resourceManagementEndpoint, subscriptionId);
                HttpResponseMessage resp = await client.PostAsync(uri, body);
                Console.WriteLine("registration status: {0}", resp.StatusCode);

                // Resource Group API 
                //StringContent body = new StringContent("{\"location\":\"West US\",\"tags\":{}}", Encoding.UTF8, "application/json");
                //string endpoint = "{0}subscriptions/{1}/resourcegroups/{2}?api-version=2014-04-01";
                
                // ADF REST API
                body = new StringContent("{\"location\":\"West US\",\"tags\":{}}", Encoding.UTF8, "application/json");
                endpoint = "{0}subscriptions/{1}/resourcegroups/{2}/providers/Microsoft.DataFactory/datafactories/{3}?api-version=2014-10-01-preview";
                uri = String.Format(endpoint, resourceManagementEndpoint, subscriptionId, resourceGroupName, dataFactoryName);
                resp = await client.PutAsync(uri, body);
                Console.WriteLine("creation status: {0}", resp.StatusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static void CallSDK(string resourceManagementEndpoint, string subscriptionId, string header, string resourceGroupName)
        {
            TokenCloudCredentials aadTokenCredentials = new TokenCloudCredentials(subscriptionId, header);
            Uri resourceManagerUri = new Uri(resourceManagementEndpoint);
            DataPipelineManagementClient client = new DataPipelineManagementClient(aadTokenCredentials, resourceManagerUri);

            // create a data factory
            Console.WriteLine("Creating a data factory");
            try
            { 
                client.DataFactories.CreateOrUpdate(resourceGroupName,
                    new DataFactoryCreateOrUpdateParameters()
                    {
                        DataFactory = new DataFactory()
                        {
                            Name = dataFactoryName,
                            Location = "westus",
                            Properties = new DataFactoryProperties() { }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static string GetAuthorizationHeader()
        {
            AuthenticationResult result = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var context = new AuthenticationContext(ConfigurationManager.AppSettings["ActiveDirectoryEndpoint"] + 
                        Environment.GetEnvironmentVariable("ACTIVEDIRECTORY_TENANT_ID", EnvironmentVariableTarget.User));

                    // show a login dialog
                    //result = context.AcquireToken(
                    //    resource: ConfigurationManager.AppSettings["ServiceManagementEndpoint"],
                    //    clientId: ConfigurationManager.AppSettings["AdfClientId"],
                    //    redirectUri: new Uri(ConfigurationManager.AppSettings["RedirectUri"]),
                    //    promptBehavior: PromptBehavior.Always);

                    // unattended
                    var credential = new UserCredential(
                        Environment.GetEnvironmentVariable("SERVICEACCOUNT_USERNAME", EnvironmentVariableTarget.User),
                        Environment.GetEnvironmentVariable("SERVICEACCOUNT_PASSWORD", EnvironmentVariableTarget.User));

                    result = context.AcquireToken(
                            resource: ConfigurationManager.AppSettings["ServiceManagementEndpoint"],
                            clientId: ConfigurationManager.AppSettings["ClientAppId"],
                            userCredential: credential); 
                }
                catch (Exception threadEx)
                {
                    Console.WriteLine(threadEx.Message);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = "AcquireTokenThread";
            thread.Start();
            thread.Join();

            if (result != null)
            {
                return result.AccessToken;
            }

            throw new InvalidOperationException("Failed to acquire token");
        }  
    }
}
