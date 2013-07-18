using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace WordSampleWebRole
{
    public class BlobUtil
    {
        public static List<string> GetAccountContainers(string account, string accesskey)
        {
            List<string> containerNames = new List<string>();
            StorageCredentialsAccountAndKey accountAndKey = new StorageCredentialsAccountAndKey(account, accesskey);
            CloudStorageAccount accountObj = new CloudStorageAccount(accountAndKey, false);
            CloudBlobClient blobClient = accountObj.CreateCloudBlobClient();
            IEnumerable<CloudBlobContainer> containers = blobClient.ListContainers();
            foreach (var c in containers)
                containerNames.Add(c.Name);
            return containerNames;
        }

        public static void SaveBlock(string account, string accesskey, string container, string fileName, Stream readStream)
        {
            StorageCredentialsAccountAndKey accountAndKey = new StorageCredentialsAccountAndKey(account, accesskey);
            CloudStorageAccount accountObj = new CloudStorageAccount(accountAndKey, false);
            CloudBlobClient blobClient = accountObj.CreateCloudBlobClient();
            CloudBlobContainer containerObj = blobClient.GetContainerReference(container);
            CloudBlockBlob blobObj = containerObj.GetBlockBlobReference(fileName);
            blobObj.UploadFromStream(readStream);
        }
    }
}