using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.SocialMedia.Common;
using Google.Apis.Auth.OAuth2;
using Google.Apis.DisplayVideo.v1;
using Google.Apis.DoubleClickBidManager.v1_1;
using Google.Apis.DoubleClickBidManager.v1_1.Data;
using Google.Apis.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class Dv360ApiCommunicator : IDv360ApiCommunicator
    {
        private readonly ILog log;
        private readonly ApplicationSettings _applicationSettings;
        readonly DoubleClickBidManagerService bidManagerservice;
        readonly DisplayVideoService displayService;
        public Dv360ApiCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                _applicationSettings = applicationSettings;
                log.Info("DV360APICommunicator Constructor");
                log.Info("DV360APICommunicator Constructor: Initialize BidManagerService Instance");
                // Get Bid Manager Service Instance
                bidManagerservice = GetBidManagerServiceInstance();
                log.Info("DV360APICommunicator Constructor: Initialize DisplayVideoService Instance");
                //Get Display Service Instance
                displayService = GetDisplayVideoServiceInstance();
            }
            catch(Exception ex)
            {
                log.Error("DV360APICommunicator Constructor :  Exception "+ ex);
                throw;
            }
        }

        /// <summary>
        /// Use this method to get creative URL
        /// </summary>
        /// <param name="advertiseID">advertiseID</param> 
        /// <param name="creativeID">creativeID</param> 
        /// <returns>creative URL</returns>
        public string GetCreativeURL(long advertiseID, long creativeID)
        {            
            try
            {
                log.Info("DV360APICommunicator GetCreativeURL: Method Start");
                if (displayService != null)
                {
                    log.Info("DV360APICommunicator GetCreativeURL: getting the creative");
                    //Get the Cretaive results based on AdvertiseId and creativeID
                    var result = displayService.Advertisers.Creatives.Get(advertiseID, creativeID).Execute();
                    if (result != null)
                    {
                        log.Info("DV360APICommunicator GetCreativeURL: retreive url from creative results");
                        //fetch the url from creative results
                        return result.ExitEvents.FirstOrDefault().Url;
                    }
                }
                else
                {
                    log.Info("DV360APICommunicator GetCreativeURL: BidManager service instance is null");
                }
            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator GetCreativeURL: Exception Found " + ex);
                
            }
            return null;
        }

        /// <summary>
        /// Use this method to get the DV360 Report URL By Query ID
        /// </summary>
        /// <param name="queryID">queryID</param>          
        /// <returns>Return the report url</returns>
        public string GetReportURLByQueryID(Enums.DV360QueryType queryType, long queryID)
        {            
            try
            {
                log.Info("DV360APICommunicator GetReportURLByQueryID: Method start");
                if (bidManagerservice != null)
                {
                    log.Info("DV360APICommunicator GetReportURLByQueryID: BidManager Service is not null, Running the query");
                    if (RunQuery(queryType, queryID))
                    {
                        log.Info("Query run Successfully , now get the results");
                        // Get the query metadata from queryID
                        var result = bidManagerservice.Queries.Getquery(queryID).Execute();

                        // Check the results.
                        if (result != null)
                        {
                            log.Info("DV360APICommunicator GetReportURLByQueryID: get query result not null");
                            //Get the Report URL from Query MetaData
                            return result.Metadata.GoogleCloudStoragePathForLatestReport;
                        }
                        else
                        {
                            log.Info("DV360APICommunicator GetReportURLByQueryID: get query result is null");
                        }
                    }
                }
                else
                {
                    log.Info("DV360APICommunicator GetReportURLByQueryID: BidManager Service Instance not created");
                }
            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator GetReportURLByQueryID: Exception Found " + ex);
                throw;
            }
            return null;
        }
        /// <summary>
        /// Use this method to create, run and get custom query report url
        /// </summary>
        /// <param name="startDateTimeMilliseconds">startDateTimeMilliseconds</param>
        /// <param name="endDateTimeMilliseconds">endDateTimeMilliseconds</param>        
        /// <returns>Return the Custom Report URL</returns>
        public string[] GetCustomReport(Enums.DV360QueryType queryType, long startDateTimeMilliseconds = 0, long endDateTimeMilliseconds = 0)
        {            
            string[] reportAndQueryID = null;            
            try
            {
                log.Info("DV360APICommunicator GetCustomReport: Method start");
                if (bidManagerservice != null)
                {
                    log.Info("DV360APICommunicator GetCustomReport: Got service instance. Now Remove Query For same QueryType ");
                    RemoveQueryForSameQueryType(queryType);

                    log.Info("DV360APICommunicator GetCustomReport: Created query object");
                    //Create Query object as per custom start and end datetime
                    Query query = CreateQuery(queryType, startDateTimeMilliseconds, endDateTimeMilliseconds);
                    log.Info("DV360APICommunicator GetCustomReport: Creating query on service account and run");
                    //Create and run the query by passing the query object
                    var result = bidManagerservice.Queries.Createquery(query).Execute();
                    log.Info("DV360APICommunicator GetCustomReport: Query Created and run");

                    //Check the results
                    if (result != null)
                    {
                        log.Info("DV360APICommunicator GetCustomReport: Create Query Result not null");
                        log.Info("DV360APICommunicator GetCustomReport: getting Report URL from get query");
                        //Get the Query Metadata from newly created query
                        var result1 = bidManagerservice.Queries.Getquery(result.QueryId.Value).Execute();

                        if (result1 != null)
                        {
                            log.Info("DV360APICommunicator GetCustomReport: Get Query Result not null");
                            // concat queryId and reportURL to make a string array
                            reportAndQueryID = new string[] { result.QueryId.Value.ToString(), result1.Metadata.GoogleCloudStoragePathForLatestReport };
                            return reportAndQueryID;
                        }
                        else
                        {
                            log.Info("DV360APICommunicator GetCustomReport: Get Query Result is null");
                        }
                    }
                    else
                    {
                        log.Info("DV360APICommunicator GetCustomReport: Create Query Result is null");
                    }
                }
                else
                {
                    log.Info("DV360APICommunicator GetCustomReport: BidManagerService Instance not created");
                }
            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator GetCustomReport: Exception Found " + ex);
                throw;
            }            
            return reportAndQueryID;
        }

        /// <summary>
        /// Use this method to delete report from DV360
        /// </summary>
        /// <param name="queryID">queryID</param>             
        /// <returns>void</returns>
        public void DeleteQuery(long queryID)
        {            
            try
            {
                log.Info("DV360APICommunicator DeleteQuery: Method Start");
                if (bidManagerservice != null)
                {
                    log.Info("DV360APICommunicator DeleteQuery: Deleting the Query");
                    //Delete the Report on DV360 by QueryID/ReportId
                    bidManagerservice.Queries.Deletequery(queryID).Execute();
                }
                else
                {
                    log.Info("DV360APICommunicator DeleteQuery: BidManager service instance is null");
                }

            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator DeleteQuery: Exception Found " + ex);
                throw;
            }
        }

        /// <summary>
        /// Use this method to Run the query by query id
        /// </summary>
        /// <param name="Enums.DV360QueryType queryType">queryType</param>
        /// <param name="queryID">queryID</param>
        /// <returns>Return bool value if run successful</returns>
        private bool RunQuery(Enums.DV360QueryType queryType, long queryID)
        {            
            try
            {
                log.Info("DV360APICommunicator RunQuery: Method start");
                log.Info("DV360APICommunicator RunQuery: creating runQuery object");
                // create RunQueryRequest for running the existing query
                RunQueryRequest runQuery = new RunQueryRequest();
                string dateRange = string.Empty;
                switch (queryType)
                {
                    //set date range 
                    case Enums.DV360QueryType.Today:
                        dateRange = Constant.dvTodayDateRange;
                        break;
                    case Enums.DV360QueryType.PreviousYear:
                        dateRange = Constant.dvPreviousYearDateRange;
                        break;
                    case Enums.DV360QueryType.YearToDate:
                        dateRange = Constant.dvYearToDateDateRange;
                        break;
                }

                if (!string.IsNullOrEmpty(dateRange))
                {
                    log.Info("DV360APICommunicator RunQuery: runQuery object created for dateRange: "+ dateRange);
                    runQuery.DataRange = dateRange;
                    // running the query
                    bidManagerservice.Queries.Runquery(runQuery, queryID).Execute();
                }
            }
            catch(Exception ex)
            {
                log.Error("DV360APICommunicator RunQuery: Exception Found: " + ex);
                throw;
            }            
            return true;
        }

        /// <summary>
        /// Use this method Remove existing Query For same QueryType
        /// </summary>
        /// <param name="Enums.DV360QueryType queryType">queryType</param>              
        private void RemoveQueryForSameQueryType(Enums.DV360QueryType queryType)
        {            
            try
            {
                log.Info("DV360APICommunicator RemoveQueryForsameQueryType: Method start");
                log.Info("DV360APICommunicator RemoveQueryForsameQueryType: Getting list of existing queries");
                //get the list of queries already exist on service account
                var queryList = bidManagerservice.Queries.Listqueries().Execute();

                if (queryList != null && queryList.Queries != null)
                {                    
                    log.Info("DV360APICommunicator RemoveQueryForsameQueryType: Iterate through existing queries");
                    //Iterate through all the query created and remove the existing query type before creating new one of same type
                    foreach (var queryResult in queryList.Queries)
                    {   
                        if (queryResult != null && ((queryType == Enums.DV360QueryType.Today && queryResult.Metadata.Title.Equals(Constant.dvTodayReportTitle)) ||
                            (queryType == Enums.DV360QueryType.PreviousYear && queryResult.Metadata.Title.Equals(Constant.dvPreviousYearReportTitle)) ||
                            (queryType == Enums.DV360QueryType.YearToDate && queryResult.Metadata.Title.Equals(Constant.dvYearToDateReportTitle)) ||
                            (queryType == Enums.DV360QueryType.Custom && queryResult.Metadata.Title.Equals(Constant.dvCustomReportTitle))))
                        {
                            log.Info("DV360APICommunicator RemoveQueryForsameQueryType: Deleting existing queryTpe = " + queryType.ToString() + " with Query ID = " + queryResult.QueryId.Value);
                            bidManagerservice.Queries.Deletequery(queryResult.QueryId.Value).Execute();
                        }
                        
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error("DV360APICommunicator RemoveQueryForsameQueryType: Exception Found " + ex);
                throw;
            }            
        }

        /// <summary>
        /// Use this method to create Custom query object
        /// </summary>
        /// <param name="startDateTimeMilliseconds">startDateTimeMilliseconds</param>
        /// <param name="endDateTimeMilliseconds">endDateTimeMilliseconds</param>
        /// <returns>Return the query object model</returns>
        private Query CreateQuery(Enums.DV360QueryType queryType, long startDateTimeMilliseconds = 0, long endDateTimeMilliseconds = 0)
        {            
            Query query = new Query();
            try
            {
                log.Info("DV360APICommunicator CreateQuery: Method start");
                query.Kind = Constant.dv360ReportKind;
                query.Metadata = new QueryMetadata();
                query.Schedule = new QuerySchedule();
                query.Params__ = new Parameters();

                if (queryType.Equals(Enums.DV360QueryType.Today))
                {
                    query.Metadata.Title = Constant.dvTodayReportTitle;
                    query.Metadata.DataRange = Constant.dvTodayDateRange;
                    query.Schedule.Frequency = Constant.dvTodayReportFrequency;                    
                }
                else if (queryType.Equals(Enums.DV360QueryType.PreviousYear))
                {
                    query.Metadata.Title = Constant.dvPreviousYearReportTitle;
                    query.Metadata.DataRange = Constant.dvPreviousYearDateRange;
                    query.Schedule.Frequency = Constant.dvPreviousYearReportFrequency;
                }
                else if (queryType.Equals(Enums.DV360QueryType.YearToDate))
                {
                    query.Metadata.Title = Constant.dvYearToDateReportTitle;
                    query.Metadata.DataRange = Constant.dvYearToDateDateRange;
                    query.Schedule.Frequency = Constant.dvYearToDateReportFrequency;
                }
                else
                {
                    query.Metadata.Title = Constant.dvCustomReportTitle;
                    query.Metadata.DataRange = Constant.dvCustomDateRangeCustom;
                    query.ReportDataStartTimeMs = startDateTimeMilliseconds;
                    query.ReportDataEndTimeMs = endDateTimeMilliseconds;
                    query.Schedule.Frequency = Constant.dvCustomReportFrequency;
                }

                query.Metadata.Format = Constant.dvReportFormat;
                List<FilterPair> filterList = new List<FilterPair>();

                if (queryType.Equals(Enums.DV360QueryType.Today))
                {
                    log.Info("DV360APICommunicator CreateQuery: Adding filterAdvertise for Delta Load");
                    FilterPair filterAdvertise = new FilterPair
                    {
                        Type = _applicationSettings.DVAdvertiseFilterType,
                        Value = _applicationSettings.DVAdvertiseFilterTypeValue_DeltaLoad
                    };
                    filterList.Add(filterAdvertise);
                }
                else
                {
                    log.Info("DV360APICommunicator CreateQuery: Adding filterAdvertise for Full Load");
                    if (!string.IsNullOrEmpty(_applicationSettings.DVAdvertiseFilterTypeValue_FullLoad))
                    {
                        string[] advertiseIds = _applicationSettings.DVAdvertiseFilterTypeValue_FullLoad.Split(',');
                        foreach (string adv in advertiseIds)
                        {
                            FilterPair filterAdvertiser = new FilterPair
                            {
                                Type = _applicationSettings.DVAdvertiseFilterType,
                                Value = adv
                            };
                            filterList.Add(filterAdvertiser);
                        }
                    }
                }

                query.Params__.Filters = filterList;
                List<string> groupBy = _applicationSettings.DVReportDimensions.Split(',').ToList();
                query.Params__.GroupBys = groupBy;
                query.Params__.IncludeInviteData = true;
                List<string> metics = _applicationSettings.DVReportMetrics.Split(',').ToList();
                query.Params__.Metrics = metics;
                query.Params__.Type = _applicationSettings.DVReportType;

            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator CreateQuery: Exception Found" + ex.Message);
                throw;
            }            
            return query;
        }

        /// <summary>
        /// Use this method to get the BID Manager Service Instance
        /// </summary>            
        /// <returns>Return the service instance</returns>
        private DoubleClickBidManagerService GetBidManagerServiceInstance()
        {            
            // Create the  BidManager Service Instance.
            try
            {
                log.Info("DV360APICommunicator GetBidManagerServiceInstance: Method Start");
                return new DoubleClickBidManagerService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GoogleCredential.FromStream(new MemoryStream(Properties.Resources.DV360APIData)).CreateScoped(
                    DoubleClickBidManagerService.Scope.Doubleclickbidmanager),
                    ApplicationName = Constant.dvApplicationName,
                });
            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator GetBidManagerServiceInstance: Exception Found " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use this method to Get DisplayVideo Service Instance Service
        /// </summary>            
        /// <returns>Return the service instance</returns>
        private DisplayVideoService GetDisplayVideoServiceInstance()
        {            
            // Create the  BidManager Service Instance.
            try
            {
                log.Info("DV360APICommunicator GetDisplayVideoServiceInstance: Method Start");
                return new DisplayVideoService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GoogleCredential.FromStream(new MemoryStream(Properties.Resources.DV360APIData)).CreateScoped(
                    DisplayVideoService.Scope.DisplayVideo),
                    ApplicationName = Constant.dvApplicationName,
                });
            }
            catch (Exception ex)
            {
                log.Error("DV360APICommunicator GetDisplayVideoServiceInstance: Exception Found " + ex.Message);
                throw;
            }
        }
    }
}
