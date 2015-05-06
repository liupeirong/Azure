using System.IO;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace SASGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Machine);
            if (storageConnectionString == null)
            {
                Console.WriteLine("Set up your Azure storage connection string in machine environment variable AZURE_STORAGE_CONNECTION_STRING.");
                Environment.Exit(1);
            }
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            //Create the blob client object.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            //Get a reference to a container to use for the sample code, and create it if it does not exist.
            string containerName = (args.Length < 1) ? "bigfiles" : args[0];
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExists();

            //Insert calls to the methods created below here...
            string sharedAccessPolicyName = "samplePolicy";
            CreateSharedAccessPolicy(blobClient, container, sharedAccessPolicyName);
            //Generate a SAS URI for the container, using a stored access policy to set constraints on the SAS.
            string sasUri = GetContainerSasUriWithPolicy(container, sharedAccessPolicyName);
            Console.WriteLine("Container SAS URI using stored access policy: " + sasUri);
        }

        static void CreateSharedAccessPolicy(CloudBlobClient blobClient, CloudBlobContainer container, string policyName)
        {
            //Create a new stored access policy and define its constraints.
            SharedAccessBlobPolicy sharedPolicy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(10),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
            };

            //Get the container's existing permissions.
            BlobContainerPermissions permissions = new BlobContainerPermissions();

            //Add the new policy to the container's permissions.
            permissions.SharedAccessPolicies.Clear();
            permissions.SharedAccessPolicies.Add(policyName, sharedPolicy);
            container.SetPermissions(permissions);
        }

        static string GetContainerSasUriWithPolicy(CloudBlobContainer container, string policyName)
        {
            //Generate the shared access signature on the container. In this case, all of the constraints for the 
            //shared access signature are specified on the stored access policy.
            string sasContainerToken = container.GetSharedAccessSignature(null, policyName);

            //Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }
    }
}
