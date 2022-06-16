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
    public static class Dv360DataMaster
    {
        [FunctionName("DV360Data_Master")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, TraceWriter _logger)
        {
            _logger.Info("C# HTTP trigger function processed a request.");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            #region Dependency Injection For Logging

            /*Following Statements are used to inject Logging dependency into the Classes of Library project, that are called
              from Function App.  In this case, ILogger is injected into the Class Library.*/
            Dependency.CreateContainer<Log4NetLoggingModule>(_logger);
            #endregion

            //Initialiazing all the objects of required classes
            _logger.Info("DV360Data_Master: _applicationSettings is going to initialize");
            ApplicationSettings _applicationSettings = Dependency.Container.Resolve<ApplicationSettings>();
            _logger.Info("DV360Data_Master: _dv360DataCommunicator is going to initialize");
            IDv360DataCommunicator _dv360DataCommunicator = Dependency.Container.Resolve<Dv360DataCommunicator>(new NamedParameter("_applicationSettings", _applicationSettings));
            _logger.Info("DV360Data_Master: _fileConvertor is going to initialize");
            IFileConvertor _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            _logger.Info("DV360Data_Master: _azureOperationsService is going to initialize");
            IAzureOperationsService _azureOperationsService = Dependency.Container.Resolve<AzureOperationsService>();
            
            //check session..
            if (await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.DVSessionFileName,
                 _applicationSettings.DVFilePath, false))
            {
                _logger.Info("DV360Data_Master: hitting while session exist..");
                return new HttpResponseMessage(HttpStatusCode.NotAcceptable);
            }
            else
            {
                _logger.Info("DV360Data_Master: session doesn't exist");
            }

            string utcDate = string.Empty;
            await Task.Run(() =>
            {
                utcDate = DateTime.UtcNow.ToString(Constant.utcDateFormat);
            });

            try
            {
                _logger.Info("DV360Data_Master: Save a new session");
                //Save session...
                await _azureOperationsService.UploadFileProcessStatus(Constant.sessionFileText, _applicationSettings.DVFilePath, _applicationSettings.DVSessionFileName);
                _logger.Info("DV360Data_Master: session saved.");

                RequestModel requestModel = new RequestModel
                {
                    FullDataCheck = Convert.ToString(req.Query["FullDataCheck"]),
                    DeltaDataCheck = Convert.ToString(req.Query["DeltaDataCheck"])
                };

                //get last run date
                _logger.Info("DV360Data_Master: Get Last Run Date");
                string dtLastRun = await _azureOperationsService.ReadFile(_applicationSettings.DVFilePath, _applicationSettings.DVLastRunFileName); // get last run  date.

                _logger.Info("DV360Data_Master: LastRunDate is: " + dtLastRun);
                _logger.Info("DV360Data_Master: getting Platform Ads data");

                // get data....                
                var adsListsdata = _dv360DataCommunicator.GetDV360AdsMasterData(dtLastRun, requestModel: requestModel);
                _logger.Info("DV360Data_Master: Platform data call has been completed and ads count is " + adsListsdata.Count);
                if (adsListsdata.Count > 0)
                {
                    _logger.Info("DV360Data_Master: Got All ads Data for Platform..");

                    var dt = _fileConvertor.ToDataTable(adsListsdata);
                    _logger.Info("DV360Data_Master: List to datatable: Done");

                    string tempFile = _fileConvertor.WriteDataTableToFile(dt, Constant.csvFileExtention);
                    _logger.Info("DV360Data_Master: WriteDataTableToFile Done");

                    //save ads data on azure
                   bool flgFileSaveStatus = await _azureOperationsService.SaveFileToAzure(tempFile, _applicationSettings.DV360AdsDataFileName, _applicationSettings.DVFilePath, true);
                    _logger.Info("DV360Data_Master: SaveFileToAzure  Done");

                    // Save Blod Date if SUCCESS only.
                    bool flgLastRunDateStatus = await _azureOperationsService.UploadFileProcessStatus(utcDate, _applicationSettings.DVFilePath, _applicationSettings.DVLastRunFileName);

                    // upload status of the run
                    bool flgStatus = await _azureOperationsService.UploadFileProcessStatus(Constant.SuccessText, _applicationSettings.DVFilePath, _applicationSettings.DVStatusFileName);
                    _logger.Info($"DV360Data_Master: Status has been saved");
                    _logger.Info($"DV360Data_Master: Completed...");
                    _logger.Info($"DV360Data_Master: Adding message to Queue Service");
                    await _azureOperationsService.MoveAllFiles(_applicationSettings.DVFilePath, Environment.GetEnvironmentVariable("MoveFilePath"));                    
                    httpResponseMessage.StatusCode = (flgFileSaveStatus && flgLastRunDateStatus && flgStatus) ? HttpStatusCode.OK : HttpStatusCode.BadRequest;                    
                }
                else
                {
                    _logger.Info("DV360Data_Master: No ads Data in Dv360");
                    await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.DVFilePath, _applicationSettings.DVStatusFileName);
                    httpResponseMessage.StatusCode = HttpStatusCode.NoContent;
                }
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                // log exception in file
                await _azureOperationsService.UploadFileProcessStatus(ex.StackTrace + " " + ex.Message, _applicationSettings.DVFilePath, Constant.exceptionFileName + utcDate + Constant.exceptionFileExtention);
                await _azureOperationsService.UploadFileProcessStatus(Constant.FailedText, _applicationSettings.DVFilePath, _applicationSettings.DVStatusFileName);

                _logger.Error("DV360Data_Master: error" + ex);
                Trace.WriteLine($"Error-->{ex.Message}");
                Trace.WriteLine($"StackTrace-->{ex.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }

            finally
            {
                _logger.Info($"DV360Data_Master: Finally block executing..");
                // Delete session.
                await _azureOperationsService.CheckFileExistAndDelete(_applicationSettings.DVSessionFileName,
                                 _applicationSettings.DVFilePath, true);
                _logger.Info($"DV360Data_Master: Session has been deleted");
            }
        }
    }
}
