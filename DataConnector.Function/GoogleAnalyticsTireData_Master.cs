using Autofac;
using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging.Log4Net;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Communicator;
using DataConnector.Intg.SocialMedia.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dependency = DataConnector.Intg.Logging.Dependency;

namespace DataConnector.Function
{
    public static class GoogleAnalyticsTireDetailsDataMaster
    {
        [FunctionName("GoogleAnalyticsTireData_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter _logger)
        {
            _logger.Info("C# HTTP trigger function processed a request.");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            _logger.Info("GATireData_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info("GATireData_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info("GATireData_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            _logger.Info("GATireData_Master: _googleTireDataCommunicator is going to initialize");
            IGoogleTireDataCommunicator _googleTireDataCommunicator = Dependency.Container.Resolve<GoogleTireDataCommunicator>(new NamedParameter("applicationSettings", _applicationSettings));
            
            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });

            try
            {
                RequestModel requestModel = new RequestModel
                {
                    FullDataCheck = "false",
                    DeltaDataCheck = "true"
                };
                //check session..
                if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.GATireSessionFileName,
                 _applicationSettings.GAFilePath, false))
                {
                    _logger.Info("GATireData_Master: hitting while session exist..");
                    return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
                }

                //Save session...
                await _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.GAFilePath, _applicationSettings.GATireSessionFileName);
                _logger.Info("GATireData_Master: session saved.");

                // get start run  date.
                string dtStartRun = Convert.ToString(req.Query["StartDate"]);
                // get dataSet name from query string
                string dataSet = Convert.ToString(req.Query["DataSetName"]);

                if (string.IsNullOrEmpty(dataSet))
                {
                    _logger.Info("GATireData_Master: dataset value is null or empty");
                    return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
                }
                
                _logger.Info("GATireData_Master: Starting.. LastRunDate?" + dtStartRun);                
                string[] listViewIDs = Environment.GetEnvironmentVariable("GACustomViewIDs").Split('|');
                int.TryParse(_applicationSettings.GADelayTime, out int delayTime);
                dataSet = dataSet.ToUpper();


                if (dataSet == Constant.RequestStatus)
                {
                    // Save status.
                    await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.GAFilePath, _applicationSettings.GATireStatusFileName);
                    // Save Date to Blob.
                    await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.GAFilePath, _applicationSettings.GATireLastRunFileName);
                    _logger.Info($"GATireData_Master: Completed...");
                    _logger.Info($"GoogleAnalytics: Adding message to Queue Service");
                    await _azureOperationsService.MoveAllFiles(_applicationSettings.GAFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));                    
                    httpResponseMessage.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    Thread.Sleep(delayTime);
                    string gaTirePageFileName = dataSet + "_" + DateTime.UtcNow.ToString(Constant.datasetDateFormat) + Constant.csvFileExtention;
                    List<GATireMasterEntity> result = _googleTireDataCommunicator.GetTireDataList(dataSet, listViewIDs, requestModel: requestModel, lastRunDate: dtStartRun);
                    if (result.Count > 0)
                    {
                        var dt = _googleTireDataCommunicator.Convertor(result, dataSet);
                        _logger.Info("GATireData_Master__fileConvertor.ToDataTable: List to datatable: Done");

                        string tempFile = _fileConvertor.WriteDataTableToFile(dt, Constant.csvFileExtention);
                        _logger.Info("GATireData_Master_fileConvertor.WriteDataTableToFile:Done");

                        bool flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, gaTirePageFileName, _applicationSettings.GAFilePath, true);
                        httpResponseMessage.StatusCode = flgFileSaveStatus ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                    }
                    else
                    {
                        _logger.Info("GATireData_Master:No  Data for GATIREPAGEDATA");
                        await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.GAFilePath, _applicationSettings.GATireStatusFileName);
                        httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                    }
                }

                return httpResponseMessage;                
            }

            catch (Exception ex)
            {
                // log exception in file
                await _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.GAFilePath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.GAFilePath, _applicationSettings.GATireStatusFileName);

                _logger.Info(": error" + ex.StackTrace, ex.Message);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            finally
            {
                _logger.Info($"GATireData_Master: Finally block executing..");
                // Delete session.
                await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.GATireSessionFileName,
                                 _applicationSettings.GAFilePath, true);

            }
        }
    }
}