using Autofac;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using System;
using System.Collections.Generic;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    /// <summary>
    /// Business Logic Class...
    /// </summary>
    public class SimilarWebDataCommunicator : ISimilarWebDataCommunicator
    {
        private readonly ILog log;
        private readonly ApplicationSettings _applicationSettings;        
        readonly ISimilarWebApiCommunicator _swAPICommunicator;        
        readonly string swApiKey;        
        public SimilarWebDataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info("SimilarWebDataCommunicator Constructor start");
                _applicationSettings = applicationSettings;
                _swAPICommunicator = Dependency.Container.Resolve<SimilarWebApiCommunicator>();
                KeyVaultService _keyService = Dependency.Container.Resolve<KeyVaultService>();
                swApiKey = _keyService.GetSecretValue(Constant.swApiKey).GetAwaiter().GetResult();
                log.Info("SimilarWebDataCommunicator Constructor End");
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebDataCommunicator Constructor: Exception " + ex);
                throw;
            }
        }

        /// <summary>
        /// get Similar Web Data
        /// </summary>
        /// <param name="startDate">start Date</param>
        /// <param name="endDate">end Date</param>        
        /// <returns>Final List of Similar Web data</returns>
        public Dictionary<string, List<SWEntity>> GetSWMasterData(string startDate, string endDate)
        {
            Dictionary<string, List<SWEntity>> swData = new Dictionary<string, List<SWEntity>>();
            List<SWEntity> swDesktopEntity = new List<SWEntity>();
            List<SWEntity> swMobileEntity = new List<SWEntity>();
            try
            {
                log.Info("SimilarWebDataCommunicator GetSWMasterData: Method started");                
                log.Info("SimilarWebDataCommunicator GetSWMasterData: startDate = " + startDate + " endDate = "+ endDate);
                int.TryParse(_applicationSettings.SWDelayTime, out int delayTime);
                bool.TryParse(_applicationSettings.SWMobileDataRequired, out bool mobileDataRequired);
                string[] dataWebsiteList = _applicationSettings.SWDataWebsites.Split(',');
                int rowNoDesktop = 0;
                int rowNoMobile = 0;
                foreach (string website in dataWebsiteList)
                {
                    log.Info("SimilarWebDataCommunicator GetSWMasterData: Get the SW Traffic source data for website:" + website);
                    string apiDesktopTrafficSourceURL = string.Empty;
                    if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
                    {
                        //Partial data of last 28 days 
                        apiDesktopTrafficSourceURL = String.Format(_applicationSettings.SWDesktopPartialTrafficSourceUrl, website, swApiKey, Constant.swSearchCountry, Constant.swDesktopGranularity);
                    }
                    else
                    {
                        // Full data for given start date and end date
                        apiDesktopTrafficSourceURL = String.Format(_applicationSettings.SWDesktopTrafficSourceUrl, website, swApiKey, startDate, endDate, Constant.swSearchCountry, Constant.swDesktopGranularity);
                    }
                    
                    string swDesktopTrafficSourceUrl = _applicationSettings.SWAPIUrl + apiDesktopTrafficSourceURL;
                    log.Info("SimilarWebDataCommunicator GetSWMasterData: swDesktopTrafficSourceUrl is: " + swDesktopTrafficSourceUrl);
                    SWDesktopTrafficSourceEntity swDesktopTrafficSourceData_List = _swAPICommunicator.GetSWDesktopTrafficeData(swDesktopTrafficSourceUrl, delayTime);
                    log.Info("SimilarWebDataCommunicator GetSWMasterData: Got desktop traffic source data");

                    if (swDesktopTrafficSourceData_List != null && swDesktopTrafficSourceData_List.DesktopOverview.Count > 0)
                    {
                        log.Info("SimilarWebDataCommunicator GetSWMasterData: Updating desktop data in final SWEntity");
                        swDesktopEntity.AddRange(UpdateDesktopData(swDesktopTrafficSourceData_List, website, ref rowNoDesktop));
                    }
                    else
                    {
                        log.Info("SimilarWebDataCommunicator GetSWMasterData: No data found for desktop for website:" + website);
                    }

                    if (mobileDataRequired)
                    {
                        GetMobileData(ref swMobileEntity, startDate, endDate, website, ref rowNoMobile, delayTime);
                    }
                }
                swData.Add(Constant.swDesktopPlatform, swDesktopEntity);
                if(mobileDataRequired)
                    swData.Add(Constant.swMobilePlatform, swMobileEntity);
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebDataCommunicator GetSWMasterData: Exception Found " + ex.Message);
                throw;
            }
            return swData;
        }

        /// <summary>
        /// Use this method to get mobile data 
        /// </summary>
        /// <param name="swEntity">swEntity</param>
        /// <param name="startDate">startDate</param>
        /// <param name="endDate">endDate</param>
        /// <param name="website">website</param>
        /// <param name="rowNo">rowNo</param>
        /// <param name="delayTime">API call delay time</param>        
        /// <returns>Return the list of SWEntity data</returns>
        private void GetMobileData(ref List<SWEntity> swEntity, string startDate, string endDate, string website, ref int rowNo, int delayTime)
        {
            try
            {
                log.Info("SimilarWebDataCommunicator GetMobileData: Method start");
                string apiMobileTrafficSourceURL = string.Empty;
                if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
                {
                    //Partial data of last 28 days 
                    apiMobileTrafficSourceURL = String.Format(_applicationSettings.SWMobilePartialTrafficeSourceUrl, website, swApiKey, Constant.swSearchCountry, Constant.swMobileGranularity);
                }
                else
                {
                    // Full data for given start date and end date
                    apiMobileTrafficSourceURL = String.Format(_applicationSettings.SWMobileTrafficeSourceUrl, website, swApiKey, startDate, endDate, Constant.swSearchCountry, Constant.swMobileGranularity);
                }

                string swMobileTrafficSourceUrl = _applicationSettings.SWAPIUrl + apiMobileTrafficSourceURL;
                log.Info("SimilarWebDataCommunicator GetMobileData: swMobileTrafficSourceUrl is: " + swMobileTrafficSourceUrl);
                SWMobileTrafficSourceEntity swMobileTrafficSourceData_List = _swAPICommunicator.GetSWMobileTrafficeData(swMobileTrafficSourceUrl, delayTime);
                log.Info("SimilarWebDataCommunicator GetMobileData: Got mobile traffic source data");

                if (swMobileTrafficSourceData_List != null && swMobileTrafficSourceData_List.MobileOverview.Count > 0)
                {
                    log.Info("SimilarWebDataCommunicator GetMobileData: Calling UpdateWebsiteURL");
                    swEntity.AddRange(UpdateMobileData(swMobileTrafficSourceData_List, website, ref rowNo));
                }
                else
                {
                    log.Info("SimilarWebDataCommunicator GetMobileData: No data found for mobile for website:" + website);
                }
            }
            catch(Exception ex)
            {
                log.Error("SimilarWebDataCommunicator GetMobileData: Exception Found " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use this method to update desktop data in SWEntity
        /// </summary>
        /// <param name="adsDataURI">Request URL</param>
        /// <param name="delayTime">API call delay time</param>        
        /// <returns>Return the list of SWEntity data</returns>
        private List<SWEntity> UpdateDesktopData(SWDesktopTrafficSourceEntity swDesktopTrafficSourceData_List, string website,ref int rowNo)
        {
            try
            {
                log.Info("SimilarWebDataCommunicator UpdateDesktopData: method start");
                List<SWEntity> swDesktopListEntity = new List<SWEntity>();                
                foreach (var overview in swDesktopTrafficSourceData_List.DesktopOverview)
                {   
                    foreach (var visit in overview.visits)
                    {
                        rowNo = rowNo + 1;
                        SWEntity enitity = new SWEntity
                        {
                            FileRowNo = rowNo,
                            Website = website,
                            Source = overview.source_type,
                            Date = visit.date,
                            Organic = visit.organic,
                            Paid = visit.paid,
                            Platform = Constant.swDesktopPlatform
                        };
                        swDesktopListEntity.Add(enitity);
                    }
                }
                log.Info("SimilarWebDataCommunicator UpdateDesktopData: method end");
                return swDesktopListEntity;
            }
            catch(Exception ex)
            {
                log.Error("SimilarWebDataCommunicator UpdateDesktopData: Exception Found " + ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to get the mobile data in SWEntity
        /// </summary>
        /// <param name="adsDataURI">Request URL</param>
        /// <param name="delayTime">API call delay time</param>        
        /// <returns>Return the list of SWEntity data</returns>
        private List<SWEntity> UpdateMobileData(SWMobileTrafficSourceEntity swMobileTrafficSourceData_List, string website, ref int rowNo)
        {
            try
            {
                log.Info("SimilarWebDataCommunicator UpdateMobileData: method start");
                List<SWEntity> swMobileListEntity = new List<SWEntity>();
                foreach (var overview in swMobileTrafficSourceData_List.MobileOverview)
                {
                    foreach (var visit in overview.visits)
                    {
                        rowNo = rowNo + 1;
                        SWEntity enitity = new SWEntity
                        {
                            FileRowNo = rowNo,
                            Website = website,
                            Source = overview.source_type,
                            Date = visit.date,
                            Organic = visit.visits,                            
                            Platform = Constant.swMobilePlatform
                        };
                        swMobileListEntity.Add(enitity);
                    }
                }
                log.Info("SimilarWebDataCommunicator UpdateMobileData: method end");
                return swMobileListEntity;
            }
            catch (Exception ex)
            {
                log.Error("SimilarWebDataCommunicator UpdateMobileData: Exception Found " + ex.Message);
                throw;
            }
        }
    }
}
