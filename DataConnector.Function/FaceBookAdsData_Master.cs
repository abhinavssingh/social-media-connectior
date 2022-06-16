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
namespace CooperTire.DigMktg.CDB.Intg.FA.BI
{
    public static class FaceBookAdsDataMaster
    {
        [FunctionName("FaceBookAdsData_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter _logger)
        {
            _logger.Info("FaceBookAdsData_Master: Run has been started..");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            //Initialiazing all the objects of required classes
            _logger.Info("FaceBookAdsData_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info("FaceBookAdsData_Master: _faceBookDataCommunicator is going to initialize");
            IFacebookDataCommunicator _faceBookDataCommunicator = Dependency.Container.Resolve<FaceBookDataCommunicator>(new NamedParameter("_applicationSettings", _applicationSettings));
            _logger.Info("FaceBookAdsData_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info("FaceBookAdsData_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            

            //check session..
            if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.FBSessionFileName,
                 _applicationSettings.FBSessionFilePath, false))
            {
                _logger.Info("FaceBookAdsData_Master: hitting while session exist..");
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            }
            else
            {
                _logger.Info("FaceBookAdsData_Master: session doesn't exist");
            }

            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });

            try
            {
                _logger.Info("FaceBookAdsData_Master: Save a new session");
                //Save session...
                _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.FBSessionFilePath, _applicationSettings.FBSessionFileName);
                _logger.Info("FaceBookAdsData_Master: session saved.");

                RequestModel requestModel = new RequestModel
                {
                    FullDataCheck = Convert.ToString(req.Query["FullDataCheck"]),
                    DeltaDataCheck = Convert.ToString(req.Query["DeltaDataCheck"])
                };

                //get last run date
                _logger.Info("FaceBookAdsData_Master: Get Last Run Date");
                string dtLastRun = await _azureOperationsService.ReadFile(_applicationSettings.FBLastRunPath, _applicationSettings.FBLastRunFileName); // get last run  date.

                _logger.Info("FaceBookAdsData_Master: LastRunDate is: " + dtLastRun);
                _logger.Info("FaceBookAdsData_Master: getting Platform Ads data");

                // get Platfrom data....
                var adsListsdata = _faceBookDataCommunicator.GetFBAdsMasterData(dtLastRun, requestModel: requestModel);
                _logger.Info("FaceBookAdsData_Master: Platform data call has been completed and ads count is " + adsListsdata.Count);
                if (adsListsdata.Count > 0)
                {
                    _logger.Info("FaceBookAdsData_Master: Got All ads Data for Platform..");

                    var dt = _fileConvertor.ToDataTable(adsListsdata);
                    _logger.Info("FaceBookAdsData_Master: List to datatable: Done");

                    string tempFile = _fileConvertor.WriteDataTableToFile(dt, Constant.csvFileExtention);
                    _logger.Info("FaceBookAdsData_Master: WriteDataTableToFile Done");

                    //save ads data on azure
                    bool flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, _applicationSettings.FBFileName, _applicationSettings.FBFilePath, true);
                    _logger.Info("FaceBookAdsData_Master: SaveFileToAzure  Done");

                    // Save Blod Date if SUCCESS only.
                    bool flgLastRunDateStatus = await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.FBLastRunPath, _applicationSettings.FBLastRunFileName);

                    // upload status of the run
                    bool flgStatus = await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.FBStatusFilePath, _applicationSettings.FBStatusFileName);
                    _logger.Info($"FaceBookAdsData_Master: Status has been saved");
                    _logger.Info($"FaceBookAdsData_Master: Completed...");
                    _logger.Info($"FaceBookAdsData_Master: Adding message to Queue Service");
                    await _azureOperationsService.MoveAllFiles(_applicationSettings.FBFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));                    
                    httpResponseMessage.StatusCode = (flgFileSaveStatus && flgLastRunDateStatus && flgStatus) ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                }
                else
                {
                    _logger.Info("FaceBookAdsData_Master: No ads Data for Platform");
                    await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.FBStatusFilePath, _applicationSettings.FBStatusFileName);
                    httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                }
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                // log exception in file
                await _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.FBExceptionPath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.FBStatusFilePath, _applicationSettings.FBStatusFileName);

                _logger.Error("FaceBookAdsData_Master: error" + ex);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }

            finally
            {
                _logger.Info($"FaceBookAdsData_Master: Finally block executing..");
                // Delete session.
                await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.FBSessionFileName,
                                 _applicationSettings.FBSessionFilePath, true);
                _logger.Info($"FaceBookAdsData_Master: Session has been deleted");
            }
        }
    }
}