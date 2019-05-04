using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace BlobAppication
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                
                string connectionString = ConfigurationManager.ConnectionStrings["AzureConnectionString"].ConnectionString;
                string folderName = "../dzFiles";
                string dContainer = "dzcontainer";
                
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient client = storageAccount.CreateCloudBlobClient();

                Console.WriteLine("Connect to conteiner");
                CloudBlobContainer container = client.GetContainerReference(dContainer);

                download(container, folderName);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
        }

        private async static void download(CloudBlobContainer container, string folder)
        {
            BlobContinuationToken dToken = null;
            do
            {
                var result = await container.ListBlobsSegmentedAsync(dToken);
                dToken = result.ContinuationToken;

                foreach (var item in result.Results)
                {
                    if (item is CloudBlobDirectory)
                    {
                        var dir = item as CloudBlobDirectory;
                        Console.WriteLine($"Start download directory:\n{dir.Prefix}");
                        Console.WriteLine(Environment.NewLine);

                        BlobContinuationToken blobToken = null;
                        var blobResult = await dir.ListBlobsSegmentedAsync(blobToken);

                        foreach (var blobItem in blobResult.Results)
                        {
                            if (blobItem is CloudBlockBlob)
                            {
                                DownloadFileFromBlob(blobItem as CloudBlockBlob, folder);
                            }
                        }
                        Console.WriteLine("\tDownload succefull!");
                    }
                    else
                    {
                        Console.WriteLine($"Start download file:\n");
                        DownloadFileFromBlob(item as CloudBlockBlob, folder, true);
                    }
                }
            } while (dToken != null);
        }

       
        private async static void DownloadFileFromBlob(CloudBlockBlob blob, string pathTo, bool fileOnly = false)
        {
            try
            {
                blob.Properties.CacheControl = (3600 * 24 * 7).ToString();

                string fileName = blob.Name;
                fileName = fileName.Replace("/", "\\");

                if(!fileOnly)
                {
                    string folderName = fileName.Split('\\')[0];
                    Directory.CreateDirectory($"{pathTo}\\{folderName}");
                }
                

                Console.WriteLine($"\tDownload file: {fileName}");

                await blob.DownloadToFileAsync($"{pathTo}/{fileName}", FileMode.Create);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }

        /// Create file uri for reading
       
        private static string CreateFileUri(CloudBlockBlob blob)
        {
            
            SharedAccessBlobPolicy constraints = new SharedAccessBlobPolicy();

            constraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(7);

            constraints.Permissions = SharedAccessBlobPermissions.Read;

            string token = blob.GetSharedAccessSignature(constraints);

            return blob.Uri + token;
        }

    }
}
