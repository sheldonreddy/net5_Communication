using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace SLS.Shared.Communication.AzureBlob
{
    public interface IBlobCommunication
    {
        Task<bool> Upload(string containerName, string fileName, string data);
        Task<string> Download(string containerName, string fileName);
        Task<bool> Delete(string containerName, string fileName);
    }

    public class BlobCommunication : IBlobCommunication
    {

        private readonly string ConnectionString; 

        public BlobCommunication(IOptions<BlobSettings> options)
        {
            ConnectionString = options.Value.ConnectionString;
        }

        public async Task<bool> Upload(string containerName, string blobName, string data)
        {
            try
            {
                var container = new BlobContainerClient(ConnectionString, containerName); ;
                var blob = container.GetBlobClient(blobName);
                var stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                await blob.UploadAsync(stream);
                return true;
            }
            catch (Exception) { return false; }
        }

        public async Task<string> Download(string containerName, string blobName)
        {
            try
            {
                var container = new BlobContainerClient(ConnectionString, containerName); ;
                var blob = container.GetBlobClient(blobName);
                var blobInfo = await blob.DownloadAsync();
                var reader = new StreamReader(blobInfo.Value.Content);
                return await reader.ReadToEndAsync();
            }
            catch (Exception) { return null; }
        }

        public async Task<bool> Delete(string containerName, string blobName)
        {
            try
            {
                var container = new BlobContainerClient(ConnectionString, containerName);
                var blob = container.GetBlobClient(blobName);
                await blob.DeleteAsync();
                return true;
            }
            catch (Exception) { return false; }
        }

    }
}
