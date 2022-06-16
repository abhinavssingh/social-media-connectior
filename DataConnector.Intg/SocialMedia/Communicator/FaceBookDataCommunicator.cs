using Autofac;
using DataConnector.Intg.Interfaces.ICommon;
using DataConnector.Intg.Interfaces.ICommunicator;
using DataConnector.Intg.Logging;
using DataConnector.Intg.SocialMedia.Common;
using DataConnector.Intg.SocialMedia.Entities;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    /// <summary>
    /// Business Loigc Class...
    /// </summary>
    public class FaceBookDataCommunicator : IFacebookDataCommunicator
    {
        private readonly ILog log;
        private readonly ApplicationSettings _applicationSettings;
        readonly ISocialHelper _socialHelper;
        readonly IFacebookApiCommunicator _facebookAPICommunicator;        
        readonly string fbAccessToken;        
        public FaceBookDataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info("FaceBookDataCommunicator Constructor start");
                _applicationSettings = applicationSettings;
                _socialHelper = Dependency.Container.Resolve<SocialHelper>(new NamedParameter("_applicationSettings", _applicationSettings));
                _facebookAPICommunicator = Dependency.Container.Resolve<FaceBookApiCommunicator>();
                KeyVaultService _keyService = Dependency.Container.Resolve<KeyVaultService>();                
                fbAccessToken = _keyService.GetSecretValue(Constant.FBAccessToken).GetAwaiter().GetResult();
                log.Info("FaceBookDataCommunicator Constructor End");
            }
            catch(Exception ex)
            {
                log.Error("FaceBookDataCommunicator Constructor: Exception "+ex);
                throw;
            }
        }

        /// <summary>
        /// get Facebook Ads data
        /// </summary>
        /// <param name="lastRunDate">last date of Run</param>
        /// <param name="requestModel">requestModel</param>        
        /// <returns>Final List of Ads data</returns>
        public List<FBAdEntity> GetFBAdsMasterData(string lastRunDate = null, RequestModel requestModel = null)
        {            
            var mrgAdsDataList = new List<AdEntity>();
            try
            {
                log.Info("FaceBookDataCommunicator GetFBAdsMasterData: Method started");
                int.TryParse(_applicationSettings.FBPlatformDayChunkValue, out int fBPlatformDayChunkValue);
                int.TryParse(_applicationSettings.FBAPICallDelay, out int delayTime);
                bool.TryParse(_applicationSettings.FBFullLoadForMonth, out bool fbFullLoadForMonth);
                bool.TryParse(requestModel.FullDataCheck, out bool fullDataCheck);
                requestModel.FullLoadForMonth = fbFullLoadForMonth;
                log.Info("FaceBookDataCommunicator GetFBAdsMasterData: fBPlatformDayChunkValue is " + fBPlatformDayChunkValue.ToString());
                var datesList = _socialHelper.GetBusinessDatesList(lastRunDate, requestModel, fBPlatformDayChunkValue, Constant.facebook);

                foreach (YearsMonths dates in datesList)
                {
                    string uriAdsDataURI = _applicationSettings.FBAdsDataURI.Replace("startDate", dates.StartDate).Replace("endDate", dates.EndDate);
                    log.Info("FaceBookDataCommunicator GetFBAdsMasterData: uriAdsDataURI is: "+ uriAdsDataURI);

                    // break down one     
                    string fbBreakdown_adsDataURI_B1 = uriAdsDataURI.Replace("{breakdownskey}", Constant.fbBreakdown_platform_platform_position);

                    string fbBrkPlatform_position = $"{_applicationSettings.FBGraphAPIUrl}/{Constant.accountIdFormat}{_applicationSettings.FBAccountID}/{fbBreakdown_adsDataURI_B1}{Constant.limit}{Constant.accessTokenFormat}{fbAccessToken}";
                    log.Info("FaceBookDataCommunicator GetFBAdsMasterData: uriAdsDataURI is: " + fbBrkPlatform_position);

                    
                    var fbBrkPlatform_platform_position_List = _facebookAPICommunicator.GetAdsDataListbyBreakDown(fbBrkPlatform_position, fullDataCheck, delayTime);
                    log.Info("FaceBookDataCommunicator GetFBAdsMasterData: Got the Facebook Platform data");
                    if (fbBrkPlatform_platform_position_List.AdsDataList.Count > 0)
                    {
                        log.Info("FaceBookDataCommunicator GetFBAdsMasterData: fbBrkPlatform_platform_position_List.AdsDataList.Count is " + fbBrkPlatform_platform_position_List.AdsDataList.Count);
                        // if next ...
                        if (!string.IsNullOrEmpty(fbBrkPlatform_platform_position_List.Paging.NextURI))
                        {
                            string nextcallURI = string.Empty;
                            do
                            {
                                log.Info("FaceBookDataCommunicator GetFBAdsMasterData: fbBrkPlatform_platform_position_List.Paging.NextURI " + fbBrkPlatform_platform_position_List.Paging.NextURI);
                                var fbBrkPlatform_platform_position_List_next = _facebookAPICommunicator.GetAdsDataListbyBreakDown(fbBrkPlatform_platform_position_List.Paging.NextURI, fullDataCheck, delayTime);
                                fbBrkPlatform_platform_position_List.AdsDataList.AddRange(fbBrkPlatform_platform_position_List_next.AdsDataList);
                                nextcallURI = fbBrkPlatform_platform_position_List_next.Paging.NextURI;

                            } while (!string.IsNullOrEmpty(nextcallURI));
                        }
                        if(fbBrkPlatform_platform_position_List.AdsDataList.Count > 0)
                            mrgAdsDataList.AddRange(fbBrkPlatform_platform_position_List.AdsDataList) ; 
                        
                        
                    }
                }
                log.Info("FaceBookDataCommunicator GetFBAdsMasterData: Calling UpdateWebsiteURL");
                // passing the ads list to update the Website URL field
                mrgAdsDataList = UpdateWebsiteURL(mrgAdsDataList);
            }
            catch (Exception ex)
            {
                log.Error("FaceBookDataCommunicator GetFBAdsMasterData: Exception Found " + ex.Message);
                throw;
            }            
            // in this method will assign the ads list values to final ads entity list
            return FaceBookDataAdsGeneratorMaster(mrgAdsDataList);
        }
        /// <summary>
        /// Update Website URL field in facebook ads data list
        /// </summary>
        /// <param name="listADEntity">list of Ads data</param>                
        /// <returns>Updated list of ads with website urls</returns>
        private List<AdEntity> UpdateWebsiteURL(List<AdEntity> listADEntity)
        {            
            try
            {
                log.Info("FaceBookDataCommunicator UpdateWebsiteURL: Method start");
                // looping through distinct adId data to get the website url 
                foreach (var adID in listADEntity.Select(item => item.AdID).Distinct())
                {   
                    log.Info("FaceBookDataCommunicator UpdateWebsiteURL: getting CreativeAdsID for adID: "+ adID);
                    // first get the creativeId using AdId
                    string fbAdsCreativeIDDataURI = string.Format(_applicationSettings.FBAdsCreativeIDDataURI, adID);
                    string fbRequestURLForCreativeAds = $"{_applicationSettings.FBGraphAPIUrl}/{fbAdsCreativeIDDataURI}{Constant.accessTokenFormatForAdsCreative}{ fbAccessToken}";
                    var responseCreativeAdsID = _facebookAPICommunicator.GetdataByURI(fbRequestURLForCreativeAds, false);
                    dynamic dataCreativeAdsID = JObject.Parse(responseCreativeAdsID);
                    GetCreativeAdsData(ref listADEntity, adID, dataCreativeAdsID);
                }
            }
            catch(Exception ex)
            {
                log.Error("FaceBookDataCommunicator UpdateWebsiteURL: Exception Found "+ ex.Message);
                throw;
            }            
            return listADEntity;
        }

        /// <summary>
        /// Get Creative Ads data and make call for further process
        /// </summary>
        /// <param name="listADEntity">list of Ads data</param>    
        /// <param name="adID">Ad id</param>    
        /// <param name="dataCreativeAdsID">dataCreativeAdsID</param>    
        /// <returns>Void</returns>
        private void GetCreativeAdsData(ref List<AdEntity> listADEntity, string adID, dynamic dataCreativeAdsID)
        {
            try
            {
                if (dataCreativeAdsID != null)
                {
                    log.Info("FaceBookDataCommunicator GetCreativeAdsData: getting PageID");
                    // if creativeId is found than get the pageid
                    string creativeID = Convert.ToString(((JObject)dataCreativeAdsID).SelectToken("data[0].id"));

                    if (!string.IsNullOrEmpty(creativeID))
                    {
                        string fbAdsPageIDDataURI = string.Format(_applicationSettings.FBAdsPageIDDataURI, creativeID);
                        string fbRequestURLForPageID = $"{_applicationSettings.FBGraphAPIUrl}/{fbAdsPageIDDataURI}{Constant.accessTokenFormat}{ fbAccessToken}";
                        var responsePageID = _facebookAPICommunicator.GetdataByURI(fbRequestURLForPageID, false);
                        dynamic dataPageID = JObject.Parse(responsePageID);

                        if (dataPageID != null)
                        {
                            log.Info("FaceBookDataCommunicator GetCreativeAdsData: getting LinkURL");
                            // if page id found than make a call to call_to_action and get the website url
                            string pageID = Convert.ToString(((JObject)dataPageID).SelectToken("effective_object_story_id"));
                            if (!string.IsNullOrEmpty(pageID))
                            {
                                string fbAdsLinkURLURI = string.Format(_applicationSettings.FBAdsLinkURLURI, pageID);
                                string fbRequestURLForLinkURL = $"{_applicationSettings.FBGraphAPIUrl}/{fbAdsLinkURLURI}{Constant.accessTokenFormat}{ fbAccessToken}";
                                var responseLinkURL = _facebookAPICommunicator.GetdataByURI(fbRequestURLForLinkURL, false);
                                dynamic dataLinkURL = JObject.Parse(responseLinkURL);
                                GetLinkDataAndUpdateLinkURL(ref listADEntity, adID, dataLinkURL);
                            }
                            else
                            {
                                log.Info("FaceBookDataCommunicator GetPageDataAndUpdateLinkURL: pageid not found");
                            }
                        }
                    }
                    else
                    {
                        log.Info("FaceBookDataCommunicator GetCreativeAdsData: creativeId not found");
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error("FaceBookDataCommunicator GetCreativeAdsData: Exception Found " + ex.Message);
                throw;
            }
            
        }

        /// <summary>
        /// Get Link data and update Website URL
        /// </summary>
        /// <param name="listADEntity">list of Ads data</param>    
        /// <param name="adID">Ad id</param>    
        /// <param name="dataLinkURL">dataLinkURL</param>    
        /// <returns>Void</returns>
        private void GetLinkDataAndUpdateLinkURL(ref List<AdEntity> listADEntity, string adID, dynamic dataLinkURL)
        {
            try
            {
                if (dataLinkURL != null)
                {
                    log.Info("FaceBookDataCommunicator GetPageDataAndUpdateLinkURL: getting websiteURL");
                    string websiteURL = Convert.ToString(((JObject)dataLinkURL).SelectToken("call_to_action.value.link"));
                    if (!string.IsNullOrEmpty(websiteURL))
                    {
                        // loop through the list and update all the items, websites url with same adId
                        log.Info("FaceBookDataCommunicator GetPageDataAndUpdateLinkURL: Updating URL in ads list");
                        foreach (var itemToChange in listADEntity.Where(d => d.AdID == adID))
                        {
                            if (itemToChange != null)
                                listADEntity[listADEntity.IndexOf(itemToChange)].WebsiteURL = websiteURL;
                        }
                    }
                    else
                    {
                        log.Info("FaceBookDataCommunicator GetPageDataAndUpdateLinkURL: websiteURL is empty");
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error("FaceBookDataCommunicator GetPageDataAndUpdateLinkURL: Exception Found " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Use this method to assign the ads data values to final entity 
        /// </summary>
        /// <param name="adsDataList">list of Ads data</param>                
        /// <returns>Return the Final Ads entity</returns>
        private List<FBAdEntity> FaceBookDataAdsGeneratorMaster(List<AdEntity> adsDataList)
        {            
            int rowNo = 0;
            List<FBAdEntity> mrgFinalAdEntities = new List<FBAdEntity>();
            try
            {
                log.Info("FaceBookDataCommunicator FaceBookDataAdsGeneratorMaster: Method Start");
                foreach (AdEntity ad in adsDataList)
                {
                    FBAdEntity adEntity = new FBAdEntity()
                    {
                        FileRowNo = ++rowNo,
                        AccountID = ad.AccountID,
                        AccountName = ad.AccountName,
                        CapaignID = ad.CapaignID,
                        CampaignName = ad.CampaignName,
                        AdSetID = ad.AdSetID,
                        AdSetName = ad.AdSetName,
                        AdID = ad.AdID,
                        AdName = ad.AdName,
                        Spend = ad.Spend,
                        Impressions = ad.Impressions,
                        Post_Engagement = ad.Post_Engagement,
                        Clicks_All = ad.Clicks_All,
                        Link_Clicks = ad.Link_Clicks,
                        Link_Clicks_Unique = ad.Link_Clicks_Unique,
                        Cost_Per_ThruPlay = ad.Cost_Per_ThruPlay != null ? ad.Cost_Per_ThruPlay.FirstOrDefault().Value : null,
                        Video_ThruPlay_Watched_Actions = ad.Video_ThruPlay_Watched_Actions != null ? ad.Video_ThruPlay_Watched_Actions.FirstOrDefault().Value : null,
                        Platform = ad.Platform,
                        Platform_Placement = ad.Platform_Placement,
                        WebsiteURL = ad.WebsiteURL,
                        Date_Start = ad.Date_Start,
                        Date_Stop = ad.Date_Stop,
                    };
                    mrgFinalAdEntities.Add(adEntity);
                }
            }
            catch(Exception ex)
            {
                log.Error("FaceBookDataCommunicator FaceBookDataAdsGeneratorMaster: Exception: " + ex.Message);
                throw;
            }            
            return mrgFinalAdEntities;
        }
    }
}
