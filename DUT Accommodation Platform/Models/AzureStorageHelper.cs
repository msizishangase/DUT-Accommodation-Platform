using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;

public static class AzureStorageHelper
{
    public static string GetLogoUrlWithSas()
    {
        var accountName = ConfigurationManager.AppSettings["AzureStorage:AccountName"];
        var accountKey = ConfigurationManager.AppSettings["AzureStorage:AccountKey"];
        var containerName = ConfigurationManager.AppSettings["AzureStorage:LogoContainer"];
        var blobName = ConfigurationManager.AppSettings["AzureStorage:LogoPath"];

        var storageAccount = CloudStorageAccount.Parse(
            $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey}");

        var blobClient = storageAccount.CreateCloudBlobClient();
        var container = blobClient.GetContainerReference(containerName);
        var blob = container.GetBlockBlobReference(blobName);

        var sasConstraints = new SharedAccessBlobPolicy
        {
            SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // Allow for clock skew
            SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1),
            Permissions = SharedAccessBlobPermissions.Read
        };

        var sasToken = blob.GetSharedAccessSignature(sasConstraints);
        return blob.Uri + sasToken;
    }
}