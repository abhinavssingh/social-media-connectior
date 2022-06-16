using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Data;
using System.Threading.Tasks;

namespace DataConnector.Intg.Interfaces.ICommon
{
    public interface IAzureOperationsService
    {
        CloudBlobClient GetCloudBlobClient();
        CloudBlobClient GetCloudBlobClient(string azureWebJobsStorage);
        CloudQueueClient GetCloudQueueClient();
        CloudFileClient GetCloudFileClient();
        Task<bool> CheckFileExistAndDelete(string fileName, string filePath, bool flgDelete);
        Task<string> ReadFile(string filePath, string fileName);
        Task<bool> MoveFile(string fileName, string sourceFilePath, string destinationFilePath, bool flgCopy);
        Task<bool> SaveFileToAzure(string tempFile, string fileName =null, string filePath=null, bool flgDelete = false);
        Task<string> DownloadExistingData(string fileName, string filePath, string fileExtension = ".csv", bool flgUseExistingData = true, bool fileNamewithExt = false);
        Task<bool> UploadFileProcessStatus(string text, string filePath, string fileName);
        Task<string> WriteDataTableFromFile(DataTable dataTable, string fileName, string filePath, bool flgUseExistingData);
        Task<bool> MoveAllFiles(string sourceBlobPath, string destinationBlobPath);
    }
}
