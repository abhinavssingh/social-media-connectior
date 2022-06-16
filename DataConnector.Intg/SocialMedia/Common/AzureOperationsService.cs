using DataConnector.Intg.Interfaces.ICommon;
using log4net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.Intg.SocialMedia.Common
{
    public class AzureOperationsService : IAzureOperationsService
    {   
        private readonly ILog log;
        readonly CloudBlobClient cloudBlobClient;
        readonly string azureWebStorage;
        readonly string azureFileJobsStorage;
        public AzureOperationsService(ILog log)
        {
            try
            {
                this.log = log;
                log.Info("AzureOperationsService Constructor");
                azureWebStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                azureFileJobsStorage = Environment.GetEnvironmentVariable("AzureFileJobsStorage");
                cloudBlobClient = GetCloudBlobClient();
                log.Info("AzureOperationsService Constructor: CloudBlobClient has been created");
                
            }
            catch(Exception ex)
            {
                log.Info("AzureOperationsService Constructor: Exception "+ ex);
                throw;
            }
        }
        /// <summary>
        /// Get CloudBlobClient
        /// </summary>                
        /// <returns>CloudBlobClient</returns>
        public CloudBlobClient GetCloudBlobClient()
        {
            try
            {
                log.Info("AzureOperationsService GetCloudBlobClient : Method start ");
                // If the connection string is valid, proceed with getting storageAccount
                if (CloudStorageAccount.TryParse(azureWebStorage, out CloudStorageAccount storageAccount))
                {
                    log.Info("AzureOperationsService GetCloudBlobClient : got storageAccount");
                    //get CloudBlobClient from storageAccount
                    return storageAccount.CreateCloudBlobClient();
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService GetCloudBlobClient: Exception Found: " + ex);
                throw;
            }
            return null;
        }

        /// <summary>
        /// Get CloudFileClient
        /// </summary>                
        /// <returns>CloudFileClient</returns>
        public CloudFileClient GetCloudFileClient()
        {
            try
            {
                log.Info("AzureOperationsService GetCloudFileClient : Method start ");
                // If the connection string is valid, proceed with getting storageAccount
                if (CloudStorageAccount.TryParse(azureFileJobsStorage, out CloudStorageAccount storageAccount))
                {
                    log.Info("AzureOperationsService GetCloudFileClient : got storageAccount");
                    //get CloudFileClient from storageAccount                    
                    return storageAccount.CreateCloudFileClient();
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService GetCloudFileClient: Exception Found: " + ex);
                throw;
            }
            return null;
        }

        /// <summary>
        /// Get CloudBlobClient by passing connection string
        /// <param>azureWebJobsStorage</param>
        /// </summary>                
        /// <returns>CloudBlobClient</returns>
        public CloudBlobClient GetCloudBlobClient(string azureWebJobsStorage)
        {
            try
            {
                log.Info("AzureOperationsService GetCloudBlobClient : Method start ");
                // If the connection string is valid, proceed with getting storageAccount
                if (CloudStorageAccount.TryParse(azureWebJobsStorage, out CloudStorageAccount storageAccount))
                {
                    log.Info("AzureOperationsService GetCloudBlobClient : got storageAccount");
                    //get CloudBlobClient from storageAccount
                    return storageAccount.CreateCloudBlobClient();
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService GetCloudBlobClient: Exception Found: " + ex);
                throw;
            }
            return null;
        }

        /// <summary>
        /// Get CloudQueueClient 
        /// </summary>               
        /// <returns>CloudQueueClient</returns>
        public CloudQueueClient GetCloudQueueClient()
        {
            try
            {
                log.Info("AzureOperationsService GetCloudQueueClient : Method start ");
                // If the connection string is valid, proceed with getting storageAccount
                if (CloudStorageAccount.TryParse(azureWebStorage, out CloudStorageAccount storageAccount))
                {
                    log.Info("AzureOperationsService GetCloudQueueClient : got storageAccount");
                    //get CloudBlobClient from storageAccount
                    return storageAccount.CreateCloudQueueClient();
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService GetCloudQueueClient: Exception Found: " + ex);
                throw;
            }
            return null;
        }
        /// <summary>
        /// Check if file exist on blob storage and provide option to delete
        /// <param>fileName</param>
        /// <param>filePath</param>
        /// <param>flgDelete</param>
        /// </summary>                
        /// <returns>bool value for file exist or not</returns>
        public async Task<bool> CheckFileExistAndDelete(string fileName, string filePath, bool flgDelete)
        {            
            bool flgExists = false;
            try
            {
                log.Info("AzureOperationsService CheckFileExistAndDelete: Method started");
                if (cloudBlobClient!= null)
                {   
                    // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                    log.Info("AzureOperationsService CheckSession: cloudBlobContainer instance creating for filePath " + filePath);
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(filePath);
                    log.Info("AzureOperationsService CheckFileExistAndDelete: getting CloudBlob for fileName " + fileName);
                    CloudBlob sourceBlob = cloudBlobContainer.GetBlobReference(fileName);

                    log.Info("AzureOperationsService CheckFileExistAndDelete: checking sourceBlob.Exists");
                    if (await sourceBlob.ExistsAsync())
                    {
                        log.Info("AzureOperationsService CheckFileExistAndDelete: sourceBlob Exists");
                        flgExists = true;

                        //remove source blob after copy is done.
                        if (flgDelete)
                        {
                            log.Info("AzureOperationsService CheckFileExistAndDelete: deleting existing sourceBlob");
                            await sourceBlob.DeleteAsync();
                            log.Info("AzureOperationsService CheckFileExistAndDelete: deleted existing sourceBlob");
                        }
                    }
                    else
                    {
                        log.Info("AzureOperationsService CheckFileExistAndDelete: sourceBlob doesn't Exists");
                    }
                }
                log.Info("AzureOperationsService CheckFileExistAndDelete: Method End");
            }
            catch(Exception ex)
            {
                log.Error("AzureOperationsService CheckFileExistAndDelete: Exception Found: " + ex.Message);
                throw;
            }
            return flgExists;
        }
        /// <summary>
        /// method is used to read the file data
        /// <param>filePath</param>
        /// <param>fileName</param>       
        /// </summary>                
        /// <returns>file data</returns>
        public async Task<string> ReadFile(string filePath, string fileName)
        {
            string strReturn = string.Empty;
            var client = new WebClient();
            try
            {
                log.Info("AzureOperationsService ReadFile: Inside Read File Method");

                // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(filePath);
                CloudBlockBlob cloudBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                if (await cloudBlob.ExistsAsync())
                {
                    byte[] buffer = client.DownloadData(cloudBlob.Uri.ToString());

                    using (var stream = new MemoryStream(buffer))
                    {
                        if (stream.Length > 0)
                        {
                            StreamReader sr = new StreamReader(stream);
                            strReturn = sr.ReadLine();
                            sr.Dispose();
                            sr.Close();
                        }
                    }
                    log.Info("AzureOperationsService ReadFile: Data is read from File");
                }
                else
                {
                    strReturn = string.Empty;
                    log.Info("AzureOperationsService ReadFile: File Does not Exists at given Path");
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService ReadFile: Exception " + ex);
                strReturn = string.Empty;
            }
            finally
            {
                client.Dispose();
            }
            return strReturn;
        }
        /// <summary>
        /// Method is used to move file from one blob to another blob
        /// <param>fileName</param>
        /// <param>sourceFilePath</param>
        /// <param>destinationFilePath</param>
        /// <param>flgCopy</param>
        /// </summary>               
        /// <returns>bool value if file moved or not</returns>
        public async Task<bool> MoveFile(string fileName, string sourceFilePath, string destinationFilePath, bool flgCopy)
        {
            try
            {
                log.Info("AzureOperationsService MoveFile: Move File started");
                bool flgStatus = false;
                if (string.IsNullOrEmpty(sourceFilePath))
                {
                    throw new Exception("AzureOperationsService MoveFile: Source blob cannot be null.");
                }
                CloudBlobContainer sourceBlobContainer = cloudBlobClient.GetContainerReference(sourceFilePath);
                CloudBlob sourceBlob = sourceBlobContainer.GetBlobReference(fileName);
                log.Info("AzureOperationsService MoveFile: Move File started For File " + sourceBlob.Uri);

                if (flgCopy)
                {
                    // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique.                     
                    CloudBlobContainer destContainer = cloudBlobClient.GetContainerReference(destinationFilePath);
                    CloudBlob destinationBlob = destContainer.GetBlobReference(fileName);
                    string copyId = await destinationBlob.StartCopyAsync(sourceBlob.Uri);
                    if (!string.IsNullOrEmpty(copyId))
                    {
                        log.Info("AzureOperationsService MoveFile: Source Blob File Copied to Destination " + destinationBlob.Uri);
                        flgStatus = true;
                    }
                    else
                    {
                        log.Info("AzureOperationsService MoveFile: Source Blob File not copied to destination location ");
                    }
                }

                //remove source blob after copy is done.
                await sourceBlob.DeleteAsync();
                log.Info("AzureOperationsService MoveFile: Source Blob File Deleted " + sourceBlob.Uri);                
                flgStatus = (flgCopy == true && flgStatus == false) ? false : true;
                return flgStatus;                
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService MoveFile: Exception " + ex);
                throw;
            }
        }
        /// <summary>
        /// Method is used to Save to file to azure blob storage and 
        /// provide option to delete the file from temp location
        /// <param>tempFile</param>
        /// <param>fileName</param>
        /// <param>filePath</param>
        /// <param>flgDelete</param>
        /// </summary>                
        /// <returns>bool value for if success or fail</returns>
        public async Task<bool> SaveFileToAzure(string tempFile, string fileName = "", string filePath = "", bool flgDelete = true)
        {            
            try
            {
                log.Info("AzureOperationsService SaveFileToAzure: Method Started");
                if (cloudBlobClient != null)
                {
                    log.Info("AzureOperationsService SaveFileToAzure: cloudBlobContainer instance creating for filePath " + filePath);
                    // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(filePath);

                    // Get a reference to the blob address, then upload the file to the blob.                    
                    log.Info("AzureOperationsService SaveFileToAzure: getting CloudBlob for fileName " + fileName);
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                    log.Info("AzureOperationsService SaveFileToAzure: uploading File to Azure from path " + tempFile);
                    await cloudBlockBlob.UploadFromFileAsync(tempFile);
                    log.Info("AzureOperationsService SaveFileToAzure: Upload File to Blob Completed");                    

                    if (flgDelete && File.Exists(tempFile))
                    {
                        log.Info("AzureOperationsService SaveFileToAzure: Deleting temp File "+ tempFile);
                        File.Delete(tempFile);
                        log.Info("AzureOperationsService SaveFileToAzure: tempFile Deleted");
                    }
                    log.Info("AzureOperationsService SaveFileToAzure: Method End");
                    return true;
                }
                log.Info("AzureOperationsService SaveFileToAzure: Method End");
            }
            catch(Exception ex)
            {
                log.Error("AzureOperationsService SaveFileToAzure: Exception Found: " + ex.Message);
                throw;
            }            
            return false;
        }
        /// <summary>
        /// Method is used to Download the file from azure blob storage        
        /// <param>fileName</param>
        /// <param>filePath</param>
        /// <param>fileExtension</param>
        /// <param>flgUseExistingData</param>
        /// <param>fileNamewithExt</param>
        /// </summary>                
        /// <returns>local temp path of file</returns>
        public async Task<string> DownloadExistingData(string fileName, string filePath, string fileExtension = ".csv", bool flgUseExistingData = true, bool fileNamewithExt = false)
        {
            string tempFilePath = string.Empty;
            try
            {
                log.Info("AzureOperationsService DownloadExistingData: Method start");
                if (cloudBlobClient != null)
                {
                    var tempPath = Path.GetTempPath(); // Get %TEMP% path
                    if (fileNamewithExt)
                    {
                        tempFilePath = Path.Combine(tempPath, fileName); // Get random file path
                    }
                    else
                    {
                        var tempFileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()); // Get random file name without extension
                        tempFilePath = Path.Combine(tempPath, tempFileName + fileExtension); // Get random file path
                    }

                    // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(filePath);
                    CloudBlob cloudBlob = cloudBlobContainer.GetBlobReference(fileName);

                    if (await cloudBlob.ExistsAsync() && flgUseExistingData)
                    {
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(cloudBlob.Uri, tempFilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService DownloadExistingData:: Exception Found: " + ex.Message);
                throw;
            }
            return tempFilePath;
        }
        /// <summary>
        /// Method is used to write text on blob file 
        /// <param>text</param>
        /// <param>filePath</param>
        /// <param>fileName</param>
        /// </summary>                
        /// <returns>bool value for if success or fail</returns>
        public async Task<bool> UploadFileProcessStatus(string text, string filePath, string fileName)
        {            
            bool isStatus = false;
            try
            {
                log.Info("AzureOperationsService UploadFileProcessStatus: Method Started");
                if (cloudBlobClient != null)
                {
                    log.Info("AzureOperationsService UploadFileProcessStatus: CloudStorageAccount instance has been created");
                    string statusPath = filePath;
                    string statusFileName = fileName;
                    
                    // Create a container called 'quickstartblobs' and append a GUID value to it to make the name unique. 
                    log.Info("AzureOperationsService UploadFileProcessStatus: cloudBlobContainer instance creating for statusPath " + statusPath);
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(statusPath);

                    // Get a reference to the blob address, then upload the file to the blob.
                    // Use the value of localFileName for the blob name.
                    log.Info("AzureOperationsService UploadFileProcessStatus: getting cloudBlockBlob for statusFileName " + statusFileName);
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(statusFileName);
                    log.Info("AzureOperationsService UploadFileProcessStatus: Uploading to blob");
                    await cloudBlockBlob.UploadTextAsync(text);
                    log.Info("AzureOperationsService SaveFileToAzure: Upload File to Blob Completed");
                    isStatus = true;
                }
                log.Info("AzureOperationsService UploadFileProcessStatus: Method End");
            }
            catch(Exception  ex)
            {
                log.Error("AzureOperationsService UploadFileProcessStatus: Exception Found: " + ex.Message);
                throw;
            }            
            return isStatus;
        }
        /// <summary>
        /// Method is used to write data in existing file from azure        
        /// <param>dataTable</param>
        /// <param>fileName</param>
        /// <param>filePath</param>
        /// <param>flgUseExistingData</param>
        /// </summary>                
        /// <returns>local temp path of file</returns>
        public async Task<string> WriteDataTableFromFile(DataTable dataTable, string fileName,string filePath, bool flgUseExistingData)
        {
            log.Info("AzureOperationsService WriteDataTableFromFile: Method start");
            bool fileExists = false;
            var existingFilePath = string.Empty;

            try
            {
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    log.Info("AzureOperationsService WriteDataTableFromFile: Datatable has Rows, processing..");

                    existingFilePath = await DownloadExistingData(fileName, filePath, ".csv" ,flgUseExistingData, true);
                    if (File.Exists(existingFilePath))
                    {
                        using (StreamReader streamReader = new StreamReader(existingFilePath))
                        {
                            if (streamReader != null && streamReader.ReadToEnd().Length > 0)
                            {
                                fileExists = true;
                            }
                        }
                    }

                    StringBuilder stringBuilder = new StringBuilder();
                    using (StreamWriter streamWriter = new StreamWriter(existingFilePath, true))
                    {
                        if (!fileExists)
                        {
                            foreach (DataColumn dataColumn in dataTable.Columns)
                            {
                                stringBuilder.Append(string.Concat(dataColumn.ColumnName, ","));
                            }
                            streamWriter.Write(stringBuilder.ToString().TrimEnd(",".ToCharArray()));
                            log.Info("AzureOperationsService WriteDataTableFromFile: Datatable Columns written on File from Path " + existingFilePath);
                        }
                        else
                        {
                            log.Info("AzureOperationsService WriteDataTableFromFile: File already exists, so no need to create columns");
                        }

                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            streamWriter.WriteLine();
                            for (int startIndex = 0; startIndex < dataTable.Columns.Count; startIndex++)
                            {
                                string strValue = ((dataRow[startIndex].ToString().Contains(",") || dataRow[startIndex].ToString().Contains(Environment.NewLine)
                                                    || dataRow[startIndex].ToString().Contains("\n")) ? string.Format("\"{0}\"", dataRow[startIndex].ToString())
                                                    : dataRow[startIndex].ToString());
                                if (startIndex == 0)
                                {
                                    streamWriter.Write(ReplaceContent(strValue));
                                }
                                else
                                {
                                    streamWriter.Write(string.Concat(",", ReplaceContent(strValue)));
                                }
                            }
                        }
                    }
                    log.Info("AzureOperationsService WriteDataTableFromFile: Datatable Rows written on File from Path " + existingFilePath);
                }
            }
            catch (Exception ex)
            {
                log.Error("AzureOperationsService WriteDataTableFromFile: Exception found " + ex);
                throw;
            }            
            return existingFilePath;
        }
        /// <summary>
        ///  Method is used to ReplaceContent - start and end with " & replace " by ""
        /// </summary>
        /// <param name="inputText"></param>        
        /// <returns>retun inputText</returns>        
        private string ReplaceContent(string inputText)
        {
            inputText = inputText.Replace("\"", "\"\""); // replace " by "".
            inputText = string.Format("\"{0}\"", inputText); //start and end with " 
            return inputText;
        }

        /// <summary>
        /// This method is used to Move all the files from source directory path to destination which have .csv extenstion and current date in file name
        /// </summary>
        /// <param name="sourceBlobPath"> Path of Source directory</param>
        /// <param name="destinationBlobPath">Path of destination directory</param>
        public async Task<bool> MoveAllFiles(string sourceBlobPath, string destinationBlobPath)
        {
            try
            {
                bool flgStatus = false;
                log.Info("AzureQueueService MoveFiles: method start");
                int index = sourceBlobPath.IndexOf('/');
                CloudBlobContainer mainContainer = cloudBlobClient.GetContainerReference(sourceBlobPath.Substring(0, index));
                CloudBlobDirectory sourceBlobDirectory = mainContainer.GetDirectoryReference(sourceBlobPath.Substring(index + 1));
                if (sourceBlobDirectory != null)
                {
                    log.Info("AzureQueueService MoveFiles: sourceBlobDirectory is not null");
                    var listBlobs = sourceBlobDirectory.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata,20, null, null, null).Result.Results.ToList();
                    log.Info("AzureQueueService MoveFiles: Got listBlobs from  sourceBlobDirectory");
                    foreach (IListBlobItem blob in listBlobs)
                    {
                        string fileName = blob.Uri.LocalPath.Split('/').Last();
                        string[] nameValues = fileName.Split('.');
                        string fileExtention = "." + nameValues[nameValues.Length - 1];
                        string date = DateTime.Now.ToString(Constant.datasetDateFormat);
                        if (fileExtention.Equals(Constant.csvFileExtention, StringComparison.OrdinalIgnoreCase) && fileName.Contains(date))
                        {
                            log.Info("AzureQueueService MoveFiles: Moving " + fileName + " from sourceBlobDirectory to destinationBlobPath");
                            flgStatus = await MoveFile(fileName, sourceBlobPath, destinationBlobPath, true);
                        }
                    }
                }
                else
                {
                    log.Info("AzureQueueService MoveFiles: sourceBlobDirectory is null");
                }
                log.Info("AzureQueueService MoveFiles: method end");
                return flgStatus;
            }
            catch (Exception ex)
            {
                log.Error("AzureQueueService MoveFiles: exception found " + ex);
                throw;
            }
        }
    }
}
