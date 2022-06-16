using Autofac;
using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    /// <summary>
    /// Business Logic Class...
    /// </summary>
    public class Dv360DataCommunicator : IDv360DataCommunicator
    {
        private readonly ILog log;
        private readonly ApplicationSettings _applicationSettings;
        readonly ISocialHelper _socialHelper;
        readonly IDv360ApiCommunicator _dv360APICommunicator;
        readonly IFileConvertor _fileConvertor;
        public Dv360DataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info("DV360DataCommunicator Constructor start");
                _applicationSettings = applicationSettings;
                _socialHelper = Dependency.Container.Resolve<SocialHelper>(new NamedParameter("_applicationSettings", _applicationSettings));
                _dv360APICommunicator = Dependency.Container.Resolve<Dv360ApiCommunicator>(new NamedParameter("_applicationSettings", _applicationSettings));
                _fileConvertor = Dependency.Container.Resolve<FileConvertor>();
                log.Info("DV360DataCommunicator Constructor End");
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator Constructor: Exception " + ex);
                throw;
            }
        }

        /// <summary>
        /// get DV360 Ads data
        /// </summary>
        /// <param name="lastRunDate">last date of Run</param>
        /// <param name="requestModel">requestModel</param>        
        /// <returns>Final List of Ads data</returns>
        public List<DV360MasterEntity> GetDV360AdsMasterData(string lastRunDate = null, RequestModel requestModel = null)
        {  
            try
            {
                log.Info("DV360DataCommunicator GetDV360AdsMasterData: Method started");
                bool.TryParse(requestModel.DeltaDataCheck, out bool deltaData);
                bool.TryParse(requestModel.FullDataCheck, out bool fullData);
                string tempFilePath = string.Empty;
                string tempFilePathYearToDate = string.Empty;

                //IF Report data need for delta Data Run 
                if (deltaData)
                {
                    //Get the Delta QueryID from Application config
                    string deltaQueryID = _applicationSettings.DVDeltaQueryID;
                    bool.TryParse(_applicationSettings.DVCreateTodayQuery, out bool createQuery);
                    tempFilePath = GetReportPath(deltaQueryID, Enums.DV360QueryType.Today, createQuery, lastRunDate, requestModel);
                }
                //Else Report data need for Full Data Run 
                else if (fullData)
                {
                    bool.TryParse(_applicationSettings.DVCreateFullQuery, out bool createQuery);
                    //Get the Full QueryIDs from Application config
                    //check if Full Query Ids exist in config
                    string queryIDPreviousYear = string.Empty;
                    string queryIDYearToDate = string.Empty;
                    if (!string.IsNullOrEmpty(_applicationSettings.DVFullQueryID))
                    {
                        string[] listFullQueries = _applicationSettings.DVFullQueryID.Split('|');
                        if (listFullQueries != null && listFullQueries.Count() > 1)
                        {
                            queryIDPreviousYear = listFullQueries[0];
                            queryIDYearToDate = listFullQueries[1];
                        }
                    }

                    if (string.IsNullOrEmpty(queryIDPreviousYear) && string.IsNullOrEmpty(queryIDYearToDate) && !createQuery)
                    {
                        tempFilePath = GetReportPath(string.Empty, Enums.DV360QueryType.Custom, createQuery, lastRunDate, requestModel);
                    }
                    else
                    {
                        tempFilePath = GetReportPath(queryIDPreviousYear, Enums.DV360QueryType.PreviousYear, createQuery, lastRunDate, requestModel);
                        tempFilePathYearToDate = GetReportPath(queryIDYearToDate, Enums.DV360QueryType.YearToDate, createQuery, lastRunDate, requestModel);
                    }

                    log.Info("DV360DataCommunicator GetDV360AdsMasterData: Get Reports(s) for Full Data");
                }
                                
                return GetDv360ModelData(tempFilePath, tempFilePathYearToDate, fullData);
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator GetDV360AdsMasterData: Exception Found " + ex);
                throw;
            }
        }

        /// <summary>
        /// get List of DV360MasterEntity
        /// </summary>
        /// <param name="tempFilePath">tempFilePath</param>
        /// <param name="tempFilePathYearToDate">tempFilePathYearToDate</param>
        /// <param name="fullData">fullData</param>
        /// <returns>Final List of Ads data</returns>
        private List<DV360MasterEntity> GetDv360ModelData(string tempFilePath, string tempFilePathYearToDate, bool fullData)
        {
            try
            {
                List<DV360Entity> mrgAdsDataList = null;
                //Check If tempFilePath is not null
                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    log.Info("DV360DataCommunicator GetDv360ModelData: Export Report to Model");
                    //Exporting data from temp file to Model
                    mrgAdsDataList = ExportCSVToModel(tempFilePath);
                    if (!string.IsNullOrEmpty(tempFilePathYearToDate))
                    {
                        //Append data from YearToDate tempFile to PreviousYear Model
                        ExportCSVToModel(tempFilePathYearToDate, mrgAdsDataList);
                    }
                    log.Info("DV360DataCommunicator GetDv360ModelData: Model updated now add URL from creative");
                    //Update the URL in Model                    
                    List<DV360Entity> dvUpdateURLData = mrgAdsDataList;
                    return SegregateData(dvUpdateURLData, fullData);
                }                
            }
            catch(Exception ex)
            {
                log.Error("DV360DataCommunicator GetDv360ModelData: Exception Found " + ex);
                throw;
            }
            return new List<DV360MasterEntity>();
        }

        /// <summary>
        /// Get temp path Report for Delta/Full Run
        /// </summary>
        /// <param name="queryIDFromConfig">queryIDFromConfig</param>
        /// <param name="queryType">queryType enums like Yesterday, PreviousYear, YearToDate</param>
        /// <param name="createQuery">createQuery bool value</param>
        /// <param name="lastRunDate">lastRunDate</param>
        /// <param name="requestModel">requestModel</param>
        /// <returns>Return Report path</returns>
        private string GetReportPath(string queryIDFromConfig, Enums.DV360QueryType queryType, bool createQuery, string lastRunDate = null, RequestModel requestModel = null)
        {
            try
            {
                log.Info("DV360DataCommunicator GetReportPath: Method start");
                string reportURL = string.Empty;
                bool isCustomQuery = false;
                Int64 queryID = 0;
                string[] reportAndQueryID = null;

                log.Info("DV360DataCommunicator GetReportPath: Get Report start");
                // If Delta/Full run for existing QueryID
                if (!string.IsNullOrEmpty(queryIDFromConfig) && !createQuery)
                {
                    log.Info("DV360DataCommunicator GetReportPath: Get Report for Query ID= " + queryIDFromConfig);
                    //Get Report URL based on QueryID
                    reportURL = _dv360APICommunicator.GetReportURLByQueryID(queryType, Convert.ToInt64(queryIDFromConfig));
                    log.Info("DV360DataCommunicator GetReportPath: Data Report URL = " + reportURL);
                }
                //Else if queryIDFromConfig blank and createQuery true than creating query based on queryType
                else if (string.IsNullOrEmpty(queryIDFromConfig) && createQuery)
                {
                    log.Info("DV360DataCommunicator GetReportPath: create Report type= " + queryType.ToString());
                    //Get Report URL with newly created query id
                    reportAndQueryID = _dv360APICommunicator.GetCustomReport(queryType);
                }
                //Else if deltaQueryID blank and createQuery false than creating custom query and get the report
                else if (string.IsNullOrEmpty(queryIDFromConfig) && !createQuery)
                {
                    log.Info("DV360DataCommunicator GetReportPath: Get Custom Report for Delta/Full");
                    isCustomQuery = true;
                    reportAndQueryID = GetCustomReportURLandQueryID(lastRunDate, requestModel);
                }
                //fetch reportURL and QueryID
                if (reportAndQueryID != null && reportAndQueryID.Count() > 1)
                {
                    queryID = Convert.ToInt64(reportAndQueryID[0]);
                    reportURL = reportAndQueryID[1];
                    log.Info("DV360DataCommunicator GetReportPath: reportURL= " + reportURL);
                    log.Info("DV360DataCommunicator GetReportPath: queryID= " + queryID + " for queryType= " + queryType.ToString());
                }

                //Download data to temp file and delete if its a custom query
                return DownloadReportAndDeleteQuery(reportURL, queryID, isCustomQuery);
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator GetCustomReportURLandQueryID: Exception Found " + ex);
                throw;
            }
        }

        /// <summary>
        /// Get Report URL and QueryID for custom report
        /// </summary>
        /// <param name="lastRunDate">lastRunDate</param>
        /// <param name="requestModel">requestModel</param>
        /// <returns>Return Report URL and QueryID</returns>
        private string[] GetCustomReportURLandQueryID(string lastRunDate = null, RequestModel requestModel = null)
        {
            try
            {
                log.Info("DV360DataCommunicator GetCustomReportURLandQueryID: Method start");
                //Get start and end dates based on requestModel
                YearsMonths dates = _socialHelper.GetBusinessDatesList(lastRunDate, requestModel, 7, Constant.dv360).FirstOrDefault();
                log.Info("DV360DataCommunicator GetCustomReportURLandQueryID: Start date=" + dates.StartDate + " and End date=" + dates.EndDate);

                DateTime.TryParse(dates.StartDate, out DateTime startDate);
                DateTime.TryParse(dates.EndDate, out DateTime endDate);
                //Convert start and end date in Miliseconds
                long startDateTimeMilliseconds = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();
                long endDateTimeMilliseconds = new DateTimeOffset(endDate).ToUnixTimeMilliseconds();

                log.Info("DV360DataCommunicator GetCustomReportURLandQueryID: startDateTimeMilliseconds=" + startDateTimeMilliseconds + " and endDateTimeMilliseconds=" + endDateTimeMilliseconds);
                //Get Report URL with newly created query
                return _dv360APICommunicator.GetCustomReport(Enums.DV360QueryType.Custom, startDateTimeMilliseconds, endDateTimeMilliseconds);
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator GetCustomReportURLandQueryID: Exception Found " + ex);
                throw;
            }
        }

        /// <summary>
        /// Download Report from reportURL and delete the custom query
        /// </summary>
        /// <param name="reportURL">reportURL</param>
        /// <param name="queryID">queryID</param>
        /// <param name="isCustomQuery">isCustomQuery</param>
        /// <returns>Return temp location path of Report</returns>
        private string DownloadReportAndDeleteQuery(string reportURL, Int64 queryID, bool isCustomQuery = false)
        {
            string tempFilePath = string.Empty;
            try
            {
                //Check If reportURL is not blank
                if (!string.IsNullOrEmpty(reportURL))
                {
                    log.Info("DV360DataCommunicator DownloadReport: Report URL is not null");
                    //Download Report at temp location based on Reprot URL
                    tempFilePath = _fileConvertor.ExportCSVExcelToFile(reportURL, ".CSV");
                    log.Info("DV360DataCommunicator DownloadReport: tempFilePath = " + tempFilePath);

                    //Check If CustomQuery created than Query need to be deleted
                    if (isCustomQuery)
                    {
                        log.Info("DV360DataCommunicator DownloadReport: isCustomQuery = " + isCustomQuery);
                        //Deleting newly custom query created
                        _dv360APICommunicator.DeleteQuery(queryID);
                        log.Info("DV360DataCommunicator DownloadReport: Custom query Deleted");
                    }
                }
                else
                {
                    log.Info("DV360DataCommunicator DownloadReport: Report URL is blank");
                }
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator DownloadReport: Exception Found " + ex);
                throw;
            }
            return tempFilePath;
        }

        /// <summary>
        /// Export data from CSV file to Model
        /// </summary>
        /// <param name="tempFilePath">tempFilePath</param>           
        /// <returns>Return Model with data</returns>
        private List<DV360Entity> ExportCSVToModel(string tempFilePath, List<DV360Entity> entityList = null)
        {
            try
            {
                log.Info("DV360DataCommunicator ExportCSVToModel: Method start");
                int rowNo;
                // initalize entityList, rowNo for first time run
                if (entityList == null)
                {
                    entityList = new List<DV360Entity>();
                    rowNo = 0;
                }
                //Else data will append in existing entityList 
                else
                {
                    // rowno start from existing model count
                    rowNo = entityList.Count;
                }

                //Reading data from temp File and populate the model
                using (StreamReader reader = new StreamReader(File.OpenRead(tempFilePath)))
                {
                    bool firstRow = true;
                    log.Info("DV360DataCommunicator ExportCSVToModel: reading csv file from location " + tempFilePath);

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (firstRow)
                        {
                            firstRow = false;
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string[] values = line.Split(',');
                            if (values.Length >= 18 && !string.IsNullOrEmpty(values[0]))
                            {
                                DV360Entity entity = new DV360Entity
                                {
                                    FileRowNo = ++rowNo,
                                    Date = values[0],
                                    LineItem = values[1],
                                    LineItemID = values[2],
                                    Campaign = values[3],
                                    CampaignID = values[4],
                                    Creative = values[5],
                                    CreativeID = values[6],
                                    FloodlightActivityName = values[7],
                                    FloodlightActivityID = values[8],
                                    AdPosition = values[9],
                                    Platform = values[10],
                                    Advertiser_ID = values[11],
                                    Impressions = values[12],
                                    Clicks = values[13],
                                    Revenue = values[14],
                                    TotalConversions = values[15],
                                    PostClickConversions = values[16],
                                    PostViewConversions = values[17]
                                };

                                entityList.Add(entity);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator ExportCSVToModel: Exception Found " + ex);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            return entityList;
        }

        /// <summary>
        /// Segregate Data based on sepration Date
        /// </summary>
        /// <param name="dvUpdateURLData">dataset dvUpdateURLData</param>
        /// <param name="fullData"> bool value fullData</param>
        /// <returns>Return Model with Final data</returns>
        private List<DV360MasterEntity> SegregateData(List<DV360Entity> dvUpdateURLData, bool fullData)
        {
            List<DV360MasterEntity> finalData = new List<DV360MasterEntity>();
            try
            {
                log.Info("DV360DataCommunicator SegregateData: Method start");
                if (fullData)
                {
                    log.Info("DV360DataCommunicator SegregateData: Segregation based on Full Data");
                    if (!string.IsNullOrEmpty(_applicationSettings.DVDataSeprationDate))
                    {
                        log.Info("DV360DataCommunicator SegregateData: Total row get from Dv360 " + dvUpdateURLData.Count);
                        DateTime.TryParse(_applicationSettings.DVDataSeprationDate, out DateTime seprationDate);

                        var dvDataBeforeSeprationDate = dvUpdateURLData.Where(x => Convert.ToDateTime(x.Date) < seprationDate && x.Advertiser_ID != _applicationSettings.DVAdvertiseFilterTypeValue_DeltaLoad).ToList();
                        log.Info("DV360DataCommunicator SegregateData: Total row from dvDataBeforeSeprationDate " + dvDataBeforeSeprationDate.Count);

                        var dvDataAfterSeprationDate = dvUpdateURLData.Where(x => Convert.ToDateTime(x.Date) >= seprationDate && x.Advertiser_ID == _applicationSettings.DVAdvertiseFilterTypeValue_DeltaLoad).ToList();
                        log.Info("DV360DataCommunicator SegregateData: Total row from dvDataAfterSeprationDate " + dvDataAfterSeprationDate.Count);

                        if (dvDataBeforeSeprationDate.Count > 0)
                        {
                            log.Info("DV360DataCommunicator SegregateData: adding data of dvDataAfterSeprationDate");
                            finalData.AddRange(ExtractCampaignData(dvDataBeforeSeprationDate, _applicationSettings.DVCampaignFilterTypeValue_CurYear));
                        }
                        if (dvDataAfterSeprationDate.Count > 0)
                        {
                            log.Info("DV360DataCommunicator SegregateData: adding data of dvDataAfterSeprationDate");
                            finalData.AddRange(ExtractCampaignData(dvDataAfterSeprationDate, _applicationSettings.DVCampaignFilterTypeValue_CurYear));
                        }
                    }
                }
                else
                {
                    log.Info("DV360DataCommunicator SegregateData: No Segregation required for delta Data");
                    finalData = ExtractCampaignData(dvUpdateURLData, _applicationSettings.DVCampaignFilterTypeValue_Today);
                }

                log.Info("DV360DataCommunicator SegregateData: Method end");
                return finalData;
            }
            catch (Exception ex)
            {
                log.Error("DV360DataCommunicator SegregateData: Exception Found " + ex);
                throw;
            }
        }

        /// <summary>
        /// Extract data on the basis of Campaigns
        /// </summary>
        /// <param name="dvCampaignData"></param>
        /// <param name="campaignFilter"></param>
        /// <returns></returns>
        private List<DV360MasterEntity> ExtractCampaignData(List<DV360Entity> dvCampaignData, string campaignFilter)
        {
            List<DV360MasterEntity> listCampaignData;

            if (string.IsNullOrEmpty(campaignFilter))
            {
                listCampaignData = dvCampaignData.OfType<DV360MasterEntity>().ToList();
            }
            else
            {
                var dvFilteredData = dvCampaignData.Where(item => campaignFilter.Contains(item.Campaign)).ToList();
                listCampaignData = dvFilteredData.OfType<DV360MasterEntity>().ToList();
            }
            return listCampaignData;
        }
    }
}
