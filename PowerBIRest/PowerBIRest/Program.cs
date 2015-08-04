using System;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Web.Script.Serialization;

namespace PowerBIRest
{
    public class LocationMap
    {
        public string location { get; set; }
        public int clicks { get; set; }
    };

    class Program
    {
        private static string datasetsUri = "https://api.powerbi.com/beta/myorg/datasets";

        private static void Main(string[] args)
        {
            var header = GetAuthorizationHeader();
            var task = CallPowerBIAPI(header);
            task.Wait();
        }

        private static async Task CallPowerBIAPI(string header)
        {

            try
            {
                var client = WebRequest.Create(datasetsUri) as HttpWebRequest;
                string response;
                //get all datasets
                client.Method = "GET";
                client.Accept = "application/json";
                client.Headers.Add("Authorization", String.Format("Bearer {0}", header));
                using (HttpWebResponse httpResponse = client.GetResponse() as System.Net.HttpWebResponse)
                {
                    //Get StreamReader that holds the response stream
                    using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                    }
                }
                Console.WriteLine("response content: {0}", response);

                //put some data
                string datasetID = "d5936a98-688a-481f-ad5d-422aaa3ce10f";
                client = WebRequest.Create(String.Format("{0}/{1}/tables/Locations/rows", datasetsUri, datasetID)) as HttpWebRequest;
                client.Method = "POST";
                client.Headers.Add("Authorization", String.Format("Bearer {0}", header));

                List<LocationMap> locations = new List<LocationMap>
                {
                    new LocationMap{location="Hongkong", clicks=4},
                    new LocationMap{location="Shanghai", clicks=3}
                };
                JavaScriptSerializer sl = new JavaScriptSerializer();
                string json = "{\"rows\":" + sl.Serialize(locations) + "}";
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
                client.ContentLength = byteArray.Length;
                client.ContentType = "application/json";

                using (Stream writer = client.GetRequestStream())
                {
                    writer.Write(byteArray, 0, byteArray.Length);
                }
                using (HttpWebResponse httpResponse = client.GetResponse() as System.Net.HttpWebResponse)
                {
                    //Get StreamReader that holds the response stream
                    using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                    }
                }
                Console.WriteLine("response content: {0}", response);

                //set some data
                // ADF REST API
                //body = new StringContent("{\"location\":\"West US\",\"tags\":{}}", Encoding.UTF8, "application/json");
                //endpoint = "{0}subscriptions/{1}/resourcegroups/{2}/providers/Microsoft.DataFactory/datafactories/{3}?api-version=2014-10-01-preview";
                //uri = String.Format(endpoint, resourceManagementEndpoint, subscriptionId, resourceGroupName, dataFactoryName);
                //resp = await client.PutAsync(uri, body);
                //Console.WriteLine("creation status: {0}", resp.StatusCode);
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

                    // unattended
                    var credential = new UserCredential(
                        Environment.GetEnvironmentVariable("SERVICEACCOUNT_USERNAME", EnvironmentVariableTarget.User),
                        Environment.GetEnvironmentVariable("SERVICEACCOUNT_PASSWORD", EnvironmentVariableTarget.User));

                    result = context.AcquireToken(
                            resource: ConfigurationManager.AppSettings["PowerBIEndpoint"],
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
