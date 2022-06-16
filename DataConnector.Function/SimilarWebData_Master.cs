using Autofac;
using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging.Log4Net;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Communicator;
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
    public static class SimilarWebDataMaster
    {
        [FunctionName("SimilarWebData_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, TraceWriter _logger)
        {
            _logger.Info("C# HTTP trigger function processed a request.");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            //Initialiazing all the objects of required classes
            _logger.Info("SimilarWebData_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info("SimilarWebData_Master: _swDataCommunicator is going to initialize");
            ISimilarWebDataCommunicator _swDataCommunicator = Dependency.Container.Resolve<SimilarWebDataCommunicator>(new NamedParameter("_applicationSettings", _applicationSettings));
            _logger.Info("SimilarWebData_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info("SimilarWebData_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            
            //check session..
            if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.SWSessionFileName,
                 _applicationSettings.SWFilePath, false))
            {
                _logger.Info("SimilarWebData_Master: hitting while session exist..");
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            }
            else
            {
                _logger.Info("SimilarWebData_Master: session doesn't exist");
            }

            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });

            try
            {
                _logger.Info("SimilarWebData_Master: Save a new session");
                //Save session...
                await _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.SWFilePath, _applicationSettings.SWSessionFileName);
                _logger.Info("SimilarWebData_Master: session saved.");

                string startDate = Convert.ToString(req.Query["startdate"]);
                string endDate = Convert.ToString(req.Query["enddate"]);

                //get last run date
                _logger.Info("SimilarWebData_Master: Get Last Run Date");
                string dtLastRun = await _azureOperationsService.ReadFile(_applicationSettings.SWFilePath, _applicationSettings.SWLastRunFileName); // get last run  date.

                _logger.Info("SimilarWebData_Master: LastRunDate is: " + dtLastRun);
                _logger.Info("SimilarWebData_Master: getting Platform Ads data");

                // get data....                
                var adsDictionarydata = _swDataCommunicator.GetSWMasterData(startDate, endDate);
                _logger.Info("SimilarWebData_Master: Platform data call has been completed and ads count is ");
                if (adsDictionarydata.Count > 0)
                {
                    bool flgFileSaveStatus = false;
                    foreach (var key in adsDictionarydata.Keys)
                    {
                        var adsListsdata = adsDictionarydata[key];

                        string fileName = key.Equals(Constant.swDesktopPlatform) ? _applicationSettings.SWDesktopDataFileName : _applicationSettings.SWMobileDataFileName;
                        _logger.Info("SimilarWebData_Master: Got All ads Data for Platform..");

                        var datatable = _fileConvertor.ToDataTable(adsListsdata);
                        _logger.Info("SimilarWebData_Master: List to datatable: Done");

                        string tempFile = _fileConvertor.WriteDataTableToFile(datatable, Constant.csvFileExtention);
                        _logger.Info("SimilarWebData_Master: WriteDataTableToFile Done");

                        //save ads data on azure
                        flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, fileName, _applicationSettings.SWFilePath, true);
                        _logger.Info("SimilarWebData_Master: SaveFileToAzure  Done");                        
                    }

                    // Save Blod Date if SUCCESS only.
                    bool flgLastRunDateStatus = await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.SWFilePath, _applicationSettings.SWLastRunFileName);

                    // upload status of the run
                    bool flgStatus = await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.SWFilePath, _applicationSettings.SWStatusFileName);
                    _logger.Info($"SimilarWebData_Master: Status has been saved");
                    _logger.Info($"SimilarWebData_Master: Completed...");
                    _logger.Info($"SimilarWebData_Master: Adding message to Queue Service");
                    await _azureOperationsService.MoveAllFiles(_applicationSettings.SWFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));
                    httpResponseMessage.StatusCode = (flgFileSaveStatus && flgLastRunDateStatus && flgStatus) ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                }
                else
                {
                    _logger.Info("SimilarWebData_Master: No ads Data on Similar web");
                    await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.SWFilePath, _applicationSettings.SWStatusFileName);
                    httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                }                
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                // log exception in file
                _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.SWFilePath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.SWFilePath, _applicationSettings.SWStatusFileName);

                _logger.Error("SimilarWebData_Master: error" + ex);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }

            finally
            {
                _logger.Info($"SimilarWebData_Master: Finally block executing..");
                // Delete session.
                _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.SWSessionFileName,
                                 _applicationSettings.SWFilePath, true);
                _logger.Info($"SimilarWebData_Master: Session has been deleted");
            }
        }
    }
}
