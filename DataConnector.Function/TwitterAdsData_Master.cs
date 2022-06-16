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
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dependency = DataConnector.Intg.Logging.Dependency;

namespace DataConnector.Function
{
    public static class TwitterAdsDataMaster
    {
        [FunctionName("TwitterAdsData_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter _logger)
        {
            _logger.Info($"TwitterAdsData_Master: Run has been started..");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            _logger.Info($"TwitterAdsData_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info($"TwitterAdsData_Master: _googleDataAPICommunicator is going to initialize");
            ITwitterDataCommunicator _twitterDataCommunicator = Dependency.Container.Resolve<TwitterDataCommunicator>(new NamedParameter("applicationSettings", _applicationSettings));
            _logger.Info($"TwitterAdsData_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info($"TwitterAdsData_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            
            //check session..
            if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.TWSessionFileName,
                 _applicationSettings.TWSessionFilePath, false))
            {
                _logger.Info($"TwitterAdsData_Master: hitting while session exist..");
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            }

            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });


            try
            {
                //Save session...
                await _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.TWSessionFilePath, _applicationSettings.TWSessionFileName);
                _logger.Info($"TwitterAdsData_Master: session saved.");

                RequestModel requestModel = new RequestModel
                {
                    FullDataCheck = Convert.ToString(req.Query["FullDataCheck"]),
                    DeltaDataCheck = Convert.ToString(req.Query["DeltaDataCheck"])
                };

                string dtLastRun = await _azureOperationsService.ReadFile(_applicationSettings.TWLastRunPath, _applicationSettings.TWLastRunFileName); // get last run  date.

                _logger.Info($"TwitterAdsData_Master: Starting.. LastRunDate?" + dtLastRun);
                //Get Campaign List
                _logger.Info($"TwitterAdsData_Master:Getting data for campaign..");
                var listCampaigns = _twitterDataCommunicator.GetCampaignList(dtLastRun);

                if (listCampaigns != null && listCampaigns.Count > 0)
                {
                    _logger.Info($"TwitterAdsData_Master: We got campaigns data and count is : " + listCampaigns.Count);
                    //Get LineItem List based on campaign
                    _logger.Info($"TwitterAdsData_Master: Getting data for Line Items..");
                    var listLineItems = _twitterDataCommunicator.GetLineItemsList(listCampaigns);
                    _logger.Info($"TwitterAdsData_Master: We got lineItems data and count is : " + listLineItems.Count);

                    var adsTwitterList = _twitterDataCommunicator.GetTwitterAdsData(listCampaigns, listLineItems, dtLastRun, requestModel);
                    if (adsTwitterList.Count > 0)
                    {
                        _logger.Info($"TwitterAdsData_Master: _fileConvertor.ToDataTable calling for converting ads list to dataTable");
                        var dt = _fileConvertor.ToDataTable(adsTwitterList);
                        _logger.Info($"TwitterAdsData_Master: List to datatable Done");

                        string tempFile = _fileConvertor.WriteDataTableToFile(dt, Constant.csvFileExtention);
                        _logger.Info($"TwitterAdsData_Master: _fileConvertor.WriteDataTableToFile Done");

                        string twPageFileName = _applicationSettings.TWFileNameForAdsData + DateTime.UtcNow.ToString(Constant.datasetDateFormat) + Constant.csvFileExtention;
                        _logger.Info($"TwitterDataCommunicator GetTwitterAdsData: twPageFileName : " + twPageFileName);
                        bool flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, twPageFileName, _applicationSettings.TWFilePath, true);

                        // Save Blod Date if SUCCESS only.
                        bool flgLastRunDateStatus = await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.TWLastRunPath, _applicationSettings.TWLastRunFileName);

                        // Save status.
                        bool flgStatus = await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.TWStatusFilePath, _applicationSettings.TWStatusFileName);
                        _logger.Info($"TwitterAdsData_Master: Completed...");
                        _logger.Info($"TwitterAdsData_Master: Adding message to Queue Service");
                        await _azureOperationsService.MoveAllFiles(_applicationSettings.TWFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));                        
                        httpResponseMessage.StatusCode = (flgFileSaveStatus && flgLastRunDateStatus && flgStatus) ? HttpStatusCode.OK : HttpStatusCode.BadRequest;                        
                    }
                    else
                    {
                        _logger.Info("FaceBookAdsData_Master: No ads Data found");
                        await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.TWStatusFilePath, _applicationSettings.TWStatusFileName);
                        httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    _logger.Info($"TwitterAdsData_Master: No Campaign data found");
                    await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.TWStatusFilePath, _applicationSettings.TWStatusFileName);
                    httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                }

                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                // log exception in file
                await _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.TWExceptionPath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.TWStatusFilePath, _applicationSettings.TWStatusFileName);

                _logger.Info($"TwitterAdsData_Master: error" + ex.StackTrace, ex.Message);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }

            finally
            {
                _logger.Info($"TwitterAdsData_Master: Finally block executing..");
                // Delete session.
                await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.TWSessionFileName,
                                 _applicationSettings.TWSessionFilePath, true);
            }
        }
    }
}