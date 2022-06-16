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
    public class GoogleDataCommunicator : IGoogleDataCommunicator
    {        
        readonly IGoogleApiCommunicator _googleAPICommunicator;
        readonly ISocialHelper _socialHelper;
        readonly ILog log;
        readonly IFileConvertor _fileConvertor;
        public GoogleDataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info("GoogleDataCommunicator Constructor");
                _googleAPICommunicator = Dependency.Container.Resolve<GoogleApiCommunicator>();
                _socialHelper = Dependency.Container.Resolve<SocialHelper>(new NamedParameter("applicationSettings", applicationSettings));
                _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
            }
            catch(Exception ex)
            {
                log.Error("GoogleDataCommunicator Constructor: Exception "+ ex);
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
        public List<GAnayticsMasterEntity> GetDataList(string dataSet, string[] listViewIDs, RequestModel requestModel = null, string lastRunDate = null)
        {
            Enum.TryParse(dataSet, out Enums.GADataType dataType);
            List<GAnayticsMasterEntity> dataList = new List<GAnayticsMasterEntity>();
            try
            {
                log.Info("GoogleDataCommunicator GetDataList : Method Start");
                //get start end data..
                log.Info("GoogleDataCommunicator GetDataList : getting dateRange where lastRunDate is: " + lastRunDate);
                YearsMonths dates = _socialHelper.GetBusinessDatesList(lastRunDate, requestModel, 7, Constant.google).FirstOrDefault();
                log.Info("GoogleDataCommunicator GetDataList : dateRange where dates.StartDate: " + dates.StartDate + " and dates.EndDate: "+ dates.EndDate);
                var dateRange = new DateRange
                {
                    StartDate = dates.StartDate,
                    EndDate = dates.EndDate
                };
                string pageToken = string.Empty;

                List<Dimension> listDimension = new List<Dimension>();
                List<Metric> listMetric = new List<Metric>();

                log.Info("GoogleDataCommunicator GetDataList : Poulate Dimension and Metric for "+ dataType.ToString());
                PopulateDimensionMetric(dataType, listDimension, listMetric);
                log.Info("GoogleDataCommunicator GetDataList : Poulated Dimension and Metric Completed");
                int rowNumber = 0;
                //loop through list of View Ids
                foreach (string viewID in listViewIDs)
                {
                    log.Info("GoogleDataCommunicator GetDataList : getting data for viewID " + viewID);
					//for a given view id loop throgh all the pages to get the data
                    while (pageToken != null)
                    {
                        log.Info("GoogleDataCommunicator GetDataList:  Calling GoogleService.ExecuteRequest Method where gaViewID: " + viewID + " and pageToken: " + pageToken);
                        // calling google api method with required parameters
                        var response = _googleAPICommunicator.ExecuteRequest(listDimension, listMetric, dateRange, viewID, pageToken);
                        log.Info("GoogleDataCommunicator GetDataList: Download data from GoogleService.ExecuteRequest Completed and got the response");

                        if (response != null && response.Reports != null && response.Reports.Count > 0 && response.Reports.First().Data != null
                            && response.Reports.First().Data.RowCount > 0)
                        {
                            log.Info("GoogleDataCommunicator GetDataList : response is not null or response.Reports have data");
                            // assign the  response data to final entity
                            switch (dataType)
                            {
                                //set date range 
                                case Enums.GADataType.GAPAGEDATA:
                                    dataList.AddRange(PageDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GADataType.GAEVENTDATA:
                                    dataList.AddRange(EventDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GADataType.GAGEODATA:
                                    dataList.AddRange(GeoDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GADataType.GAPAGEEVENTDATA:
                                    dataList.AddRange(PageEventDataListCollection(response, ref rowNumber));
                                    break;
                                case Enums.GADataType.GACUSTOMDATA:                                    
                                    dataList.AddRange(CustomDataListCollection(response, ref rowNumber));
                                    break;
                            }                                                       
                            log.Info("GoogleDataCommunicator GetDataList : NextPageToken is " + response.Reports.First().NextPageToken);
                            //assign the Next page Token for pagination data
                            pageToken = response.Reports.First().NextPageToken;
                        }
                        else
                        {
                            pageToken = null;
                        }
                    }
                    pageToken = string.Empty;
                    log.Info("GoogleDataCommunicator GetDataList : DownloadDataCall has been completed for viewID " + viewID);                    
                }
            }
            catch(Exception ex)
            {
                log.Error("GoogleDataCommunicator GetDataList : Exception: " + ex.Message);
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
        private void PopulateDimensionMetric(Enums.GADataType dataType, List<Dimension> listDimension, List<Metric> listMetric)
        {            
            try
            {
                log.Info("GoogleDataCommunicator PoulateDimensionMetric : Method start");
                List<string> listDmn = new List<string>();
                List<string> listMtc =  new List<string>();
                log.Info("GoogleDataCommunicator PoulateDimensionMetric : Fetch dimension and metric data from Constant");
                switch (dataType)
                {
                    //set date range 
                    case Enums.GADataType.GAPAGEDATA:
                        listDmn = Constant.gaPageDimensions.Split(',').ToList();
                        listMtc = Constant.gaPageMetric.Split(',').ToList();                        
                        break;
                    case Enums.GADataType.GAEVENTDATA:
                        listDmn = Constant.gaEventDimensions.Split(',').ToList();
                        listMtc = Constant.gaEventMetric.Split(',').ToList();
                        break;
                    case Enums.GADataType.GAGEODATA:
                        listDmn = Constant.gaGeoDimensions.Split(',').ToList();
                        listMtc = Constant.gaGeoMetric.Split(',').ToList();
                        break;
                    case Enums.GADataType.GAPAGEEVENTDATA:
                        listDmn = Constant.gaPageEventDimensions.Split(',').ToList();
                        listMtc = Constant.gaPageEventMetric.Split(',').ToList();
                        break;
                    case Enums.GADataType.GACUSTOMDATA:
                        listDmn = Constant.gaCustomDimensions.Split(',').ToList();
                        listMtc = Constant.gaCustomMetric.Split(',').ToList();
                        break;                    
                }

                log.Info("GoogleDataCommunicator PoulateDimensionMetric : adding Dimensions to listDimension obj");
                foreach (string dmn in listDmn)
                {
                    var Dimension = new Dimension
                    {
                        Name = "ga:" + dmn
                    };
                    listDimension.Add(Dimension);
                }
                log.Info("GoogleDataCommunicator PoulateDimensionMetric : adding Metric to listMetric obj");
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
                log.Error("GoogleDataCommunicator PoulateDimensionMetric : Exception Found " + ex);
                throw;
            }            
        }

        /// <summary>
        /// Use this method to assign the google analytics response to Page entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final Page analytics entity</returns>
        private List<GAnayticsMasterEntity> PageDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GAnalyticsPageEntity> pageDataList = new List<GAnalyticsPageEntity>();
            try
            {
                log.Info("GoogleDataCommunicator PageDataListCollection : Method Start");
                log.Info("GoogleDataCommunicator PageDataListCollection : Mapping data foreach rows to GAnalyticsPageEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GAnalyticsPageEntity gAnalyticsPageData = new GAnalyticsPageEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = (x.Dimensions[1]),
                        ChannelGrouping = (x.Dimensions[2]),
                        Hostname = (x.Dimensions[3]),
                        PagePath = (x.Dimensions[4]),
                        PageTitle = (x.Dimensions[5]),
                        SocialNetwork = (x.Dimensions[6]),
                        AdContent = (x.Dimensions[7]),
                        Campaign = (x.Dimensions[8]),

                        Sessions = x.Metrics.First().Values[0],
                        PageViews = x.Metrics.First().Values[1],
                        UniquePageViews = x.Metrics.First().Values[2],
                        Entrances = x.Metrics.First().Values[3],
                        Bounces = x.Metrics.First().Values[4]
                    };

                    pageDataList.Add(gAnalyticsPageData);
                }
                log.Info("GoogleDataCommunicator PageDataListCollection : Mapping to GAnalyticsPageEntity Model has been completed");
            }
            catch(Exception ex)
            {
                log.Error("GoogleDataCommunicator PageDataListCollection : Exception: " + ex.Message);
                throw;
            }            
            List<GAnayticsMasterEntity> gaData = pageDataList.OfType<GAnayticsMasterEntity>().ToList<GAnayticsMasterEntity>();
            return gaData;
        }

        /// <summary>
        /// Use this method to assign the google analytics response to Event entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final event analytics entity</returns>
        private List<GAnayticsMasterEntity> EventDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GAnalyticsEventEntity> eventDataList = new List<GAnalyticsEventEntity>();
            try
            {
                log.Info("GoogleDataCommunicator EventDataListCollection : Method Start");
                log.Info("GoogleDataCommunicator EventDataListCollection : Mapping data foreach rows to GAnalyticsEventEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GAnalyticsEventEntity gAnalyticsEventData = new GAnalyticsEventEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = (x.Dimensions[1]),
                        ChannelGrouping = (x.Dimensions[2]),
                        Hostname = (x.Dimensions[3]),
                        EventCategory = (x.Dimensions[4]),
                        EventAction = (x.Dimensions[5]),
                        EventLabel = (x.Dimensions[6]),
                        SocialNetwork = (x.Dimensions[7]),

                        Sessions = x.Metrics.First().Values[0],
                        UniqueEvents = x.Metrics.First().Values[1],
                        TotalEvents = x.Metrics.First().Values[2],
                        SessionsWithEvent = x.Metrics.First().Values[3]
                    };


                    eventDataList.Add(gAnalyticsEventData);
                }
                log.Info("GoogleDataCommunicator EventDataListCollection : Mapping to GAnalyticsEventEntity Model has been completed");
            }
            catch (Exception ex)
            {
                log.Error("GoogleDataCommunicator EventDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GAnayticsMasterEntity> gaData = eventDataList.OfType<GAnayticsMasterEntity>().ToList<GAnayticsMasterEntity>();            
            return gaData;

        }

        /// <summary>
        /// Use this method to assign the google analytics response to geo entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final geo analytics entity</returns>
        private List<GAnayticsMasterEntity> GeoDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GAnalyticsGeoEntity> geoDataList = new List<GAnalyticsGeoEntity>();
            try
            {
                log.Info("GoogleDataCommunicator GeoDataListCollection : Method Start");
                log.Info("GoogleDataCommunicator GeoDataListCollection : Mapping data foreach rows to GAnalyticsGeoEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GAnalyticsGeoEntity gAnalyticsGeoData = new GAnalyticsGeoEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = (x.Dimensions[1]),
                        ChannelGrouping = (x.Dimensions[2]),
                        Hostname = (x.Dimensions[3]),
                        City = (x.Dimensions[4]),
                        Region = (x.Dimensions[5]),
                        SocialNetwork = (x.Dimensions[6]),

                        Sessions = x.Metrics.First().Values[0],
                        UniquePageViews = x.Metrics.First().Values[1]
                    };

                    geoDataList.Add(gAnalyticsGeoData);
                }
                log.Info("GoogleDataCommunicator GeoDataListCollection : Mapping to GAnalyticsGeoEntity Model has been completed");
            }

            catch (Exception ex)
            {
                log.Error("GoogleDataCommunicator GeoDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GAnayticsMasterEntity> gaData = geoDataList.OfType<GAnayticsMasterEntity>().ToList<GAnayticsMasterEntity>();            
            return gaData;
        }

        /// <summary>
        /// Use this method to assign the google analytics response to custom entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final custom analytics entity</returns>
        private List<GAnayticsMasterEntity> CustomDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GAnalyticsCustomEntity> customDataList = new List<GAnalyticsCustomEntity>();
            try
            {
                log.Info("GoogleDataCommunicator CustomDataListCollection : Method Start");
                log.Info("GoogleDataCommunicator CustomDataListCollection : Mapping data foreach rows to GAnalyticsCustomEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GAnalyticsCustomEntity gAnalyticsCustomData = new GAnalyticsCustomEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = (x.Dimensions[1]),
                        ChannelGrouping = (x.Dimensions[2]),
                        Hostname = (x.Dimensions[3]),
                        SocialNetwork = (x.Dimensions[4]),

                        HelpMeChooseComplete = x.Metrics.First().Values[0],
                        FindAStoreInteraction = x.Metrics.First().Values[1],
                        ProductPageViewTireDetails = x.Metrics.First().Values[2]
                    };

                    customDataList.Add(gAnalyticsCustomData);
                }
                log.Info("GoogleDataCommunicator CustomDataListCollection : Mapping to GAnalyticsCustomEntity Model has been completed");
            }
            catch (Exception ex)
            {
                log.Error("GoogleDataCommunicator CustomDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GAnayticsMasterEntity> gaData = customDataList.OfType<GAnayticsMasterEntity>().ToList<GAnayticsMasterEntity>();            
            return gaData;
        }

        /// <summary>
        /// Use this method to assign the google analytics response to page event entity list
        /// </summary>
        /// <param name="response">response data of google analytics</param>        
        /// <param name="rowNumber">Row Number</param>
        /// <returns>Return the list of Final page event analytics entity</returns>
        private List<GAnayticsMasterEntity> PageEventDataListCollection(GetReportsResponse response, ref int rowNumber)
        {            
            List<GAnalyticsPageEventEntity> pageEventDataList = new List<GAnalyticsPageEventEntity>();
            try
            {
                log.Info("GoogleDataCommunicator PageEventDataListCollection : Method Start");
                log.Info("GoogleDataCommunicator PageEventDataListCollection : Mapping data foreach rows to GAnalyticsEventEntity Model from response.Reports.First().Data.Rows");
                foreach (var x in response.Reports.First().Data.Rows)
                {
                    GAnalyticsPageEventEntity gAnalyticsPageEventData = new GAnalyticsPageEventEntity
                    {
                        FileRowNo = ++rowNumber,
                        Date = (x.Dimensions[0]),
                        Country = (x.Dimensions[1]),
                        ChannelGrouping = (x.Dimensions[2]),
                        Hostname = (x.Dimensions[3]),
                        EventCategory = (x.Dimensions[4]),
                        EventAction = (x.Dimensions[5]),
                        EventLabel = (x.Dimensions[6]),
                        SocialNetwork = (x.Dimensions[7]),
                        PagePath = (x.Dimensions[8]),

                        Sessions = x.Metrics.First().Values[0],
                        UniqueEvents = x.Metrics.First().Values[1],
                        TotalEvents = x.Metrics.First().Values[2],
                        SessionsWithEvent = x.Metrics.First().Values[3]
                    };


                    pageEventDataList.Add(gAnalyticsPageEventData);
                }
                log.Info("GoogleDataCommunicator PageEventDataListCollection : Mapping to GAnalyticsPageEventEntity Model has been completed");
            }
            catch (Exception ex)
            {
                log.Error("GoogleDataCommunicator PageEventDataListCollection : Exception:" + ex.Message);
                throw;
            }
            List<GAnayticsMasterEntity> gaData = pageEventDataList.OfType<GAnayticsMasterEntity>().ToList<GAnayticsMasterEntity>();            
            return gaData;
        }        
        public DataTable Convertor(List<GAnayticsMasterEntity> result, string dataSet)
        {
            Enum.TryParse(dataSet, out Enums.GADataType dataType);
            DataTable dataTable = new DataTable();
            switch (dataType)
            {
                //set date range 
                case Enums.GADataType.GAPAGEDATA:
                    dataTable = ProcessData<GAnalyticsPageEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GADataType.GAEVENTDATA:
                    dataTable = ProcessData<GAnalyticsEventEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GADataType.GAGEODATA:
                    dataTable = ProcessData<GAnalyticsGeoEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GADataType.GAPAGEEVENTDATA:
                    dataTable = ProcessData<GAnalyticsPageEventEntity>.GetData(result, _fileConvertor);
                    break;
                case Enums.GADataType.GACUSTOMDATA:
                    dataTable = ProcessData<GAnalyticsCustomEntity>.GetData(result, _fileConvertor);
                    break;
            }

            return dataTable;
        }
        
    }

    public static class ProcessData<T> where T : class
    {
        public static DataTable GetData(List<GAnayticsMasterEntity> result, IFileConvertor _fileConvertor)
        {
            List<T> pageEventDataList = result.OfType<T>().ToList();
            var dt = _fileConvertor.ToDataTable(pageEventDataList);
            return dt;
        }
    }
}
