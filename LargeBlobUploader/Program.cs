namespace LargeFileUploader
{
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Text;

    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("largefileuploader.exe <local file path> <container SAS string> <optional: upload chunk size in KB>");
                Environment.Exit(1);
            }
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            int chunkSizeKB = (args.Length > 2) ? int.Parse(args[2]) : 2048; 
            LargeFileUploaderUtils.NumBytesPerChunk = chunkSizeKB * 1024;

            //LargeFileUploaderUtils.UploadAsync(
            //    inputFile: @"C:\temp\watestsample.bak",
            //    storageConnectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Machine),
            //    containerName: "bigfiles",
            //    uploadParallelism: 2).Wait();

            LargeFileUploaderUtils.UploadAsyncSAS(
                inputFile: args[0],
                containerSASString: args[1],
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