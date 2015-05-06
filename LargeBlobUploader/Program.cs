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
            LargeFileUploaderUtils.NumBytesPerChunk = 2 * 1024 * 1024;

            //LargeFileUploaderUtils.UploadAsync(
            //    inputFile: @"C:\temp\watestsample.bak",
            //    storageConnectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Machine),
            //    containerName: "bigfiles",
            //    uploadParallelism: 2).Wait();

            LargeFileUploaderUtils.UploadAsyncSAS(
                inputFile: @"C:\temp\Microsoft_Intune_Setup.zip",
                containerSASString: @"https://schneiderstore.blob.core.windows.net/bigfiles?sv=2012-02-12&sr=c&si=samplePolicy&sig=E7m9f4Udq2ztBrwhSOPmSbcR8G5qu1g39To6WwJ3%2Bus%3D",
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