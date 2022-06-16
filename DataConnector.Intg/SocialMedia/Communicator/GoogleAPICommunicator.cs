using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.SocialMedia.Common;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class GoogleApiCommunicator : IGoogleApiCommunicator
    {
        private readonly ILog log;
        public GoogleApiCommunicator(ILog log)
        {            
            this.log = log;            
        }

        /// <summary>
        /// Use this method to get google analytics data
        /// </summary>
        /// <param name="listDimension">Dimension list</param>
        /// <param name="listMetric">metric list</param>
        /// <param name="dateRange">date range</param>
        /// <param name="gaViewID">View Id</param>
        /// <param name="pageToken">Page Token</param>        
        /// <returns>Return the list of google analytics data</returns>
        public GetReportsResponse ExecuteRequest(List<Dimension> listDimension, List<Metric> listMetric, DateRange dateRange,
                                                    string gaViewID, string pageToken)
        {            
            try
            {
                log.Info("GoogleAPICommunicator ExecuteRequest : Method Start");
                log.Info("GoogleAPICommunicator ExecuteRequest : Creating ReportRequest object");
                var reportRequest = new ReportRequest
                {
                    DateRanges = new List<DateRange> { dateRange },
                    Dimensions = listDimension,
                    Metrics = listMetric,
                    ViewId = gaViewID,
                    PageSize = 100000,
                    PageToken = pageToken.ToString()
                };
                log.Info("GoogleAPICommunicator ExecuteRequest : ReportRequest object created");
                var getReportsRequest = new GetReportsRequest
                {
                    ReportRequests = new List<ReportRequest> { reportRequest }
                };
                log.Info("GoogleAPICommunicator ExecuteRequest : Get Analytics Reporting Service Instance");
                //create batch with AnalyticsReportingService for the Report request
                var batchRequest = GetAnalyticsReportingServiceInstance().Reports.BatchGet(getReportsRequest);
                log.Info("GoogleAPICommunicator ExecuteRequest :Executing batchRequest.Execute()");
				//finally execute the batch request to get the response data
                var response = batchRequest.Execute();
                log.Info("GoogleAPICommunicator ExecuteRequest :Executed batchRequest");
                log.Info("GoogleAPICommunicator ExecuteRequest : Method End");
                return response;
            }
            catch(Exception ex)
            {
                log.Error("GoogleAPICommunicator ExecuteRequest : Exception:" + ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to create AnalyticsReportingService instance
        /// </summary>            
        /// <returns>Return the instance of AnalyticsReportingService</returns>
        private AnalyticsReportingService GetAnalyticsReportingServiceInstance()
        {            
            // Create the  Analytics service.
            try
            {
                log.Info("GoogleAPICommunicator GetAnalyticsReportingServiceInstance : Method Start");
                return new AnalyticsReportingService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GoogleCredential.FromStream(new MemoryStream(Properties.Resources.GAAPIData)).CreateScoped(
                    AnalyticsReportingService.Scope.AnalyticsReadonly),
                    ApplicationName = Constant.GAServiceAccount,
                });
            }
            catch(Exception ex)
            {
                log.Error("GoogleAPICommunicator GetAnalyticsReportingServiceInstance : Exception Found "+ ex.Message);
                throw;
            }
        }
    }
}
