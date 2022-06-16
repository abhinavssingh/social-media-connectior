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

namespace CooperTire.DigMktg.CDB.Intg.FA.BI
{
    public static class GoogleAnalyticsDataMaster
    {
        [FunctionName("GoogleAnalyticsData_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, TraceWriter _logger)
        {
            _logger.Info("C# HTTP trigger function processed a request.");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            _logger.Info("GoogleAnalyticsData_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info("GoogleAnalyticsData_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info("GoogleAnalyticsData_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            _logger.Info("GoogleAnalyticsData_Master: _googleDataAPICommunicator is going to initialize");
            IGoogleDataCommunicator _googleDataCommunicator = Dependency.Container.Resolve<GoogleDataCommunicator>(new NamedParameter("applicationSettings", _applicationSettings));
            
            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });

            try
            {
                RequestModel requestModel = new RequestModel
                {
                    FullDataCheck = Convert.ToString(req.Query["FullDataCheck"]),
                    DeltaDataCheck = Convert.ToString(req.Query["DeltaDataCheck"])
                };
                // get dataSet name from query string
                string dataSet = Convert.ToString(req.Query["DataSetName"]);

                if (string.IsNullOrEmpty(dataSet))
                {
                    _logger.Info("dataSet value is null or empty");
                    return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
                }

                //check session..
                if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.GASessionFileName,
                 _applicationSettings.GASessionFilePath, false))
                {
                    _logger.Info("GoogleAnalytics: hitting while session exist..");
                    return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
                }

                //Save session...
                await _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.GASessionFilePath, _applicationSettings.GASessionFileName);
                _logger.Info("GoogleAnalytics: session saved.");


                string dtLastRun = await _azureOperationsService.ReadFile(_applicationSettings.GALastRunPath, _applicationSettings.GALastRunFileName);
                // get last run  date.

                _logger.Info(": Starting.. LastRunDate?" + dtLastRun);

                string[] listViewIDs = Environment.GetEnvironmentVariable("GAViewIDs").Split('|');
                string[] listCustomViewIDs = Environment.GetEnvironmentVariable("GACustomViewIDs").Split('|');                
                string[] viewIds = dataSet == Enums.GADataType.GACUSTOMDATA.ToString() ? listCustomViewIDs : listViewIDs;
                bool fullData = false;
                bool.TryParse(requestModel.FullDataCheck, out fullData);
                int.TryParse(_applicationSettings.GADelayTime, out int delayTime);
                dataSet = dataSet.ToUpper();

                if (dataSet == Constant.RequestStatus)
                {
                    // Save status.
                    await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.GAStatusFilePath, _applicationSettings.GAStatusFileName);
                    // Save Date to Blob.
                    await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.GALastRunPath, _applicationSettings.GALastRunFileName);
                    _logger.Info($"GoogleAnalytics: Completed...");
                    _logger.Info($"GoogleAnalytics: Adding message to Queue Service");
                    await _azureOperationsService.MoveAllFiles(_applicationSettings.GAFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));                    
                    httpResponseMessage.StatusCode = HttpStatusCode.OK;
                }
                else
                {
                    if (fullData)
                    {
                        Thread.Sleep(delayTime);
                    }

                    string gaEventFileName = dataSet + "_" + DateTime.UtcNow.ToString(Constant.datasetDateFormat) + Constant.csvFileExtention;

                    List<GAnayticsMasterEntity> result = _googleDataCommunicator.GetDataList(dataSet, viewIds, requestModel: requestModel, lastRunDate: dtLastRun);
                    if (result.Count > 0)
                    {
                        var dt = _googleDataCommunicator.Convertor(result, dataSet);
                        _logger.Info("GoogleAnalytics__fileConvertor.ToDataTable: List to datatable: Done");

                        string tempFile = _fileConvertor.WriteDataTableToFile(dt, Constant.csvFileExtention);
                        _logger.Info("GoogleAnalytics_fileConvertor.WriteDataTableToFile:Done");

                        bool flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, gaEventFileName, _applicationSettings.GAFilePath, true);
                        httpResponseMessage.StatusCode = flgFileSaveStatus ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                    }
                    else
                    {
                        _logger.Info("No  Data for Google Analytics");
                        await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.GAStatusFilePath, _applicationSettings.GAStatusFileName);
                        httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                    }
                }
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                // log exception in file
                await _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.GAExceptionPath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.GAStatusFilePath, _applicationSettings.GAStatusFileName);

                _logger.Info(": error" + ex.StackTrace, ex.Message);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            finally
            {
                _logger.Info($"GoogleAnalyticsData_Master: Finally block executing..");
                // Delete session.
                await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.GASessionFileName,
                                 _applicationSettings.GASessionFilePath, true);

            }
        }
    }
}

