using Autofac;
using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Entities;
using Google.Apis.AnalyticsReporting.v4.Data;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class GoogleTireDataCommunicator : IGoogleTireDataCommunicator
    {        
        readonly IGoogleApiCommunicator _googleAPICommunicator;
        readonly ISocialHelper _socialHelper;
        readonly ILog log;
        readonly IFileConvertor _fileConvertor;
        public GoogleTireDataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info("GoogleTireDataCommunicator Constructor");
                _googleAPICommunicator = Dependency.Container.Resolve<GoogleApiCommunicator>();
                _socialHelper = Dependency.Container.Resolve<SocialHelper>(new NamedParameter("applicationSettings", applicationSettings));
                _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            }
            catch(Exception ex)
            {
                log.Error("GoogleTireDataCommunicator Constructor: Exception " + ex);
                throw;
            }
        }

        /// <summary>
        /// This method return the list of analytics entity data
        /// </summary>
        /// <param name="listViewIDs">list of View IDs</param>
        /// <param name="requestModel">requestModel</param> 
        /// <param name="lastRunDate">Date of last run</param>
        /// <returns>Return the list analytics entity</returns>
        public List<GATireMasterEntity> GetTireDataList(string dataSet, string[] listViewIDs, RequestModel requestModel = null, string lastRunDate = null)
        {
            Enum.TryParse(dataSet, out Enums.GATireDataType dataType);
            List<GATireMasterEntity> dataList = new List<GATireMasterEntity>();
            try
            {
                log.Info("GoogleTireDataCommunicator GetDataList : Method Start");
                //get start end data..
                log.Info("GoogleTireDataCommunicator GetDataList : getting dateRange where lastRunDate is: " + lastRunDate);
                YearsMonths dates = _socialHelper.GetBusinessDatesList(lastRunDate, requestModel, 7, Constant.google).FirstOrDefault();
                log.Info("GoogleTireDataCommunicator GetDataList : dateRange where dates.StartDate: " + dates.StartDate + " and dates.EndDate: "+ dates.EndDate);
                var dateRange = new DateRange
                {
                    StartDate = dates.StartDate,
                    EndDate = dates.EndDate
                };
                string pageToken = string.Empty;

                List<Dimension> listDimension = new List<Dimension>();
                List<Metric> listMetric = new List<Metric>();

                log.Info("GoogleTireDataCommunicator GetDataList : Poulate Dimension and Metric for " + dataType.ToString());
                PopulateDimensionMetric(dataType, listDimension, listMetric);
                log.Info("GoogleTireDataCommunicator GetDataList : Poulated Dimension and Metric Completed");
                int rowNumber = 0;
                //loop through list of View Ids
                foreach (string viewID in listViewIDs)
                {
                    log.Info("GoogleTireDataCommunicator GetDataList : getting data for viewID " + viewID);
					//for a given view id loop throgh all the pages to get the data
                    while (pageToken != null)
                    {
                        log.Info("GoogleTireDataCommunicator GetDataList:  Calling GoogleService.ExecuteRequest Method where gaViewID: " + viewID + " and pageToken: " + pageToken);
                        // calling google api method with required parameters
                        var response = _googleAPICommunicator.ExecuteRequest(listDimension, listMetric, dateRange, viewID, pageToken);
                        log.Info("GoogleTireDataCommunicator GetDataList: Download data from GoogleService.ExecuteRequest Completed and got the response");

                        if (response != null && response.Reports != null && response.Reports.Count > 0 && response.Reports.First().Data != null
                            && response.Reports.First().Data.RowCount > 0)
                        {
                            log.Info("GoogleTireDataCommunicator GetDataList : response is not null or response.Reports have data");
                            // assign the  response data to final entity
                            switch (dataType)
                            {
                                //set date range 
                                case Enums.GATireDataType.GATIREPAGEDATA:
                                    dataList.AddRange(PageDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GATireDataType.GATIREPAGEEVENTDATA:
                                    dataList.AddRange(EventDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GATireDataType.GATIRECUSTOMDATA:                                    
                                    dataList.AddRange(CustomDataListCollection(response, ref rowNumber));
                                    break;
                            }                                                       
                            log.Info("GoogleTireDataCommunicator GetDataList : NextPageToken is " + response.Reports.First().NextPageToken);
                            //assign the Next page Token for pagination data
                            pageToken = response.Reports.First().NextPageToken;
                        }
                        else
                        {
                            pageToken = null;
                        }
                    }
                    pageToken = string.Empty;
                    log.Info("GoogleTireDataCommunicator GetDataList : DownloadDataCall has been completed for viewID " + viewID);                    
                }
            }
            catch(Exception ex)
            {
                log.Error("GoogleTireDataCommunicator GetDataList : Exception: " + ex.Message);
                throw;
            }            
            return dataList;
        }

        /// <summary>
        /// Use this method to assign the Dimension & Metric list
        /// </summary>
        /// <param name="dataType">Enums.gaDataType dataType</param>        
        /// <param name="listDimension">Dimension List</param>
        /// <param name="listMetric">Metric List</param>
        /// <returns>void</returns>
        private void PopulateDimensionMetric(Enums.GATireDataType dataType, List<Dimension> listDimension, List<Metric> listMetric)
        {            
            try
            {
                log.Info("GoogleTireDataCommunicator PoulateDimensionMetric : Method start");
                List<string> listDmn = new List<string>();
                List<string> listMtc =  new List<string>();
                log.Info("GoogleTireDataCommunicator PoulateDimensionMetric : Fetch dimension and metric data from Constant");
                switch (dataType)
                {
                    //set date range 
                    case Enums.GATireDataType.GATIREPAGEDATA:
                        listDmn = Constant.gaTirePageDimensions.Split(',').ToList();
                        listMtc = Constant.gaTirePageMetric.Split(',').ToList();                        
                        break;
                    case Enums.GATireDataType.GATIREPAGEEVENTDATA:
                        listDmn = Constant.gaTireEventDimensions.Split(',').ToList();
                        listMtc = Constant.gaTireEventMetric.Split(',').ToList();
                        break;
                    case Enums.GATireDataType.GATIRECUSTOMDATA:
                        listDmn = Constant.gaTireCustomDimensions.Split(',').ToList();
                        listMtc = Constant.gaTireCustomMetric.Split(',').ToList();
                        break;    
                }

                log.Info("GoogleTireDataCommunicator PoulateDimensionMetric : adding Dimensions to listDimension obj");
                foreach (string dmn in listDmn)
                {
                    var Dimension = new Dimension
                    {
                        Name = "ga:" + dmn
                    };
                    listDimension.Add(Dimension);
                }
                log.Info("GoogleTireDataCommunicator PoulateDimensionMetric : adding Metric to listMetric obj");
                foreach (string data in listMtc)
                {
                    string[] metric = data.Split('|');
                    var Metric = new Metric
                    {
                        Expression = "ga:" + metric[0],
                        Alias = metric[1]
                    };
                    listMetric.Add(Metric);
                }
            }
            catch(Exception ex)
            {
                log.Error("GoogleTireDataCommunicator PoulateDimensionMetric : Exception Found " + ex);
                throw;
            }            
        }

        /// <summary>
        /// Use this method to assign the google analytics response to Page entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final Page analytics entity</returns>
        private List<GATireMasterEntity> PageDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GATirePageEntity> pageDataList = new List<GATirePageEntity>();
            try
            {
                log.Info("GoogleTireDataCommunicator PageDataListCollection : Method Start");
                log.Info("GoogleTireDataCommunicator PageDataListCollection : Mapping data foreach rows to GATirePageEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GATirePageEntity gAnalyticsPageData = new GATirePageEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = x.Dimensions[1],
                        Hostname = x.Dimensions[2],

                        UniquePageViews = x.Metrics.First().Values[0]                        
                    };

                    pageDataList.Add(gAnalyticsPageData);
                }
                log.Info("GoogleTireDataCommunicator PageDataListCollection : Mapping to GATirePageEntity Model has been completed");
            }
            catch(Exception ex)
            {
                log.Error("GoogleTireDataCommunicator PageDataListCollection : Exception: " + ex.Message);
                throw;
            }            
            List<GATireMasterEntity> gaData = pageDataList.OfType<GATireMasterEntity>().ToList<GATireMasterEntity>();
            return gaData;
        }

        /// <summary>
        /// Use this method to assign the google analytics response to Event entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final event analytics entity</returns>
        private List<GATireMasterEntity> EventDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GATireEventEntity> eventDataList = new List<GATireEventEntity>();
            try
            {
                log.Info("GoogleTireDataCommunicator EventDataListCollection : Method Start");
                log.Info("GoogleTireDataCommunicator EventDataListCollection : Mapping data foreach rows to GATireEventEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GATireEventEntity gAnalyticsEventData = new GATireEventEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = x.Dimensions[1],
                        Hostname = x.Dimensions[2],
                        DefaultChannelGrouping = (x.Dimensions[3]),
                        PagePath = (x.Dimensions[4]),
                        EventCategory = (x.Dimensions[5]),
                        EventAction = (x.Dimensions[6]),
                        EventLabel = (x.Dimensions[7]), 
                        
                        UniqueEvents = x.Metrics.First().Values[0]
                    };


                    eventDataList.Add(gAnalyticsEventData);
                }
                log.Info("GoogleTireDataCommunicator EventDataListCollection : Mapping to GATireEventEntity Model has been completed");
            }
            catch (Exception ex)
            {
                log.Error("GoogleTireDataCommunicator EventDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GATireMasterEntity> gaData = eventDataList.OfType<GATireMasterEntity>().ToList<GATireMasterEntity>();            
            return gaData;

        }       

        /// <summary>
        /// Use this method to assign the google analytics response to custom entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final custom analytics entity</returns>
        private List<GATireMasterEntity> CustomDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GATireCustomEntity> customDataList = new List<GATireCustomEntity>();
            try
            {
                log.Info("GoogleTireDataCommunicator CustomDataListCollection : Method Start");
                log.Info("GoogleTireDataCommunicator CustomDataListCollection : Mapping data foreach rows to GATireCustomEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GATireCustomEntity gAnalyticsCustomData = new GATireCustomEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = x.Dimensions[1],
                        Hostname = x.Dimensions[2],
                        DefaultChannelGrouping = (x.Dimensions[3]),
                        CustomDimension16 =x.Dimensions[4],                        

                        UniquePageViews = x.Metrics.First().Values[0]
                    };

                    customDataList.Add(gAnalyticsCustomData);
                }
                log.Info("GoogleTireDataCommunicator CustomDataListCollection : Mapping to GATireCustomEntity Model has been completed");
            }
            catch (Exception ex)
            {
                log.Error("GoogleTireDataCommunicator CustomDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GATireMasterEntity> gaData = customDataList.OfType<GATireMasterEntity>().ToList<GATireMasterEntity>();            
            return gaData;
        }

        public DataTable Convertor(List<GATireMasterEntity> result, string dataSet)
        {
            Enum.TryParse(dataSet, out Enums.GATireDataType dataType);
            DataTable dataTable = new DataTable();
            switch (dataType)
            {
                //set date range 
                case Enums.GATireDataType.GATIREPAGEDATA:
                    dataTable = ProcessTireData<GATirePageEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GATireDataType.GATIREPAGEEVENTDATA:
                    dataTable = ProcessTireData<GATireEventEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GATireDataType.GATIRECUSTOMDATA:
                    dataTable = ProcessTireData<GATireCustomEntity>.GetData(result, _fileConvertor);
                    break;                
            }

            return dataTable;
        }
    }

    public static class ProcessTireData<T> where T : class
    {
        public static DataTable GetData(List<GATireMasterEntity> result, IFileConvertor _fileConvertor)
        {
            List<T> pageEventDataList = result.OfType<T>().ToList();
            var dt = _fileConvertor.ToDataTable(pageEventDataList);
            return dt;
        }
    }
}
