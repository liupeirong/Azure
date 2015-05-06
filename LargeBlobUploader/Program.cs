namespace LargeFileUploader
{
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Text;

    class Program
    {

        static void Main(string[] args)
        {
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            LargeFileUploaderUtils.NumBytesPerChunk = 8 * 1024;

            LargeFileUploaderUtils.UploadAsync(
                inputFile: @"C:\temp\watestsample.bak",
                storageConnectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Machine),
                containerName: "bigfiles",
                uploadParallelism: 2).Wait();


            //byte[] someData = Encoding.UTF8.GetBytes("Hallo");

            //var address = someData.UploadAsync(
            //    storageAccount: CloudStorageAccount.DevelopmentStorageAccount,
            //    containerName: "dummy222222",
            //    blobName: "somedata2.txt",
            //    uploadParallelism: 1).Result;
        }
    }
}