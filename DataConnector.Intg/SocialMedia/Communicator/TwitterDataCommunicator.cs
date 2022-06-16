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
using System.IO;
using System.Linq;

namespace DataConnector.Intg.SocialMedia.Communicator
{
    public class TwitterDataCommunicator : ITwitterDataCommunicator
    {
        private readonly ILog log;
        private readonly ApplicationSettings _applicationSettings;
        readonly ITwitterApiCommunicator _twitterAPICommunicator;
        readonly ISocialHelper _twitterServices;
        
        public TwitterDataCommunicator(ApplicationSettings applicationSettings, ILog log)
        {
            try
            {
                this.log = log;
                log.Info($"TwitterDataCommunicator Constructor start");
                _applicationSettings = applicationSettings;                
                _twitterAPICommunicator = Dependency.Container.Resolve<TwitterApiCommunicator>();
                _twitterServices = new SocialHelper(_applicationSettings, log);
                log.Info($"TwitterDataCommunicator Constructor End");
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator Constructor: Exception "+ ex);
                throw;
            }
        }

        public List<TWFinalAdsEntity> GetTwitterAdsData(List<TWCampaignEntity> listCampaigns, List<TWLineItemEntity> listLineItems, string dtLastRun, RequestModel requestModel)
        { 
            try
            {
                log.Info($"TwitterDataCommunicator GetTwitterAdsData: Method Start");
                List<TWFinalAdsEntity> twitterAdsData = new List<TWFinalAdsEntity>();
                if (listLineItems != null && listLineItems.Count > 0)
                {                    
                    //Get Promoted Tweets List based on LineItem
                    log.Info($"TwitterDataCommunicator GetTwitterAdsData: Getting data for Promoted Tweets..");
                    var listPromotedTweets = GetPromotedTweetsList(listLineItems);

                    if (listPromotedTweets != null && listPromotedTweets.Count > 0)
                    {
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: We got Promoted Tweets data and count is : " + listPromotedTweets.Count);
                        //Get Tweets based on Promoted Tweets
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: Getting data for Tweets..");
                        var listTweets = GetTweetsList(listPromotedTweets);
                        log.Info($"GetTwitterAdsData: We got Tweets data and count is : " + listTweets.Count);

                        // ALL_ON_TWITTER..         
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: Getting data for ALL_ON_Twitter..");
                        var adsData_ALL_ON_TWITTER = GetAdsMasterDataList(Constant.TwPlacementAll, dtLastRun, requestModel, listPromotedTweets, listTweets);
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: We got ALL_ON_TWITTER Ads data and count is : " + adsData_ALL_ON_TWITTER.Count);

                        // PUBLISHER_NETWORK...
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: Getting data for Publisher Network..");
                        var adsData_PUBLISHER_NETWORK = GetAdsMasterDataList(Constant.TwPlacementPublisher, dtLastRun, requestModel, listPromotedTweets, listTweets);
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: We got PUBLISHER_NETWORK Ads data and count is : " + adsData_PUBLISHER_NETWORK.Count);

                        if (adsData_ALL_ON_TWITTER.Count > 0 || adsData_PUBLISHER_NETWORK.Count > 0)
                        {   
                            log.Info($"TwitterDataCommunicator GetTwitterAdsData: Mapping all the data to make final list");
                            twitterAdsData = (GetFinalTwitterAdsDataList(listCampaigns, listLineItems, listPromotedTweets, adsData_ALL_ON_TWITTER, adsData_PUBLISHER_NETWORK));
                            log.Info($"TwitterDataCommunicator GetTwitterAdsDatasData_Master: Final list has been mapped");
                        }
                        else
                        {
                            log.Info($"TwitterDataCommunicator GetTwitterAdsData: No ads Data for TWITTER");
                        }
                    }
                    else
                    {
                        log.Info($"TwitterDataCommunicator GetTwitterAdsData: No Promoted Tweets data found based on LineItems IDs");
                    }
                }
                else
                {
                    log.Info($"TwitterDataCommunicator GetTwitterAdsData: No Line Items data found based on Campaign IDs");
                }
                return twitterAdsData;
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetCampaignList: Exception Found " + ex.Message);
                throw;
            }        
        }

        /// <summary>
        /// Use this method to get all the campaigns based on delta/full load
        /// </summary>
        /// <param name="lastRunDate">date of last run</param>
        /// <param name="requestModel">requestModel</param>        
        /// <returns>Return the list of campaigns</returns>
        public List<TWCampaignEntity> GetCampaignList(string lastRunDate = null)
        {            
            List<TWCampaignEntity> listFinalCampaigns = new List<TWCampaignEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetCampaignList: Method started");
                List<TWCampaignEntity> listAllCampaigns = new List<TWCampaignEntity>(); 
                //assembling the request url by paasing required parameters 
                string apiURLParameters = String.Format(_applicationSettings.TWAdsCampaignDataURI, _applicationSettings.TWAccountID);
                string resourceUrl = _applicationSettings.TWAdsAPIUrl + apiURLParameters;
                log.Info($"TwitterDataCommunicator GetCampaignList: Request URL is "+ resourceUrl);
                //calling api communicator for the response data
                var response = _twitterAPICommunicator.GetResponse(resourceUrl, Method.GET);
                log.Info($"TwitterDataCommunicator GetCampaignList: Get the response");
                using (var sr = new StreamReader(response.GetResponseStream()))
                {                       
                    dynamic result = JObject.Parse(sr.ReadToEnd());
                    JArray resultData = (JArray)result.SelectToken("data");
                    log.Info($"TwitterDataCommunicator GetCampaignList: Adding response to campaign entity");
                    //add the response data to campaign list
                    AddCampaignToList(listAllCampaigns, resultData);
                        
                    //check if request have pagination data
                    if (!string.IsNullOrEmpty(Convert.ToString(result.next_cursor)))
                    {
                        log.Info($"TwitterDataCommunicator GetCampaignList: Getting Pagination data for the request");
                        string nextCursor = Convert.ToString(result.next_cursor);
                        do
                        {
                            string resourceUrlCursor = resourceUrl + "?cursor=" + nextCursor;
                            log.Info($"TwitterDataCommunicator GetCampaignList: resourceUrlCursor: "+ resourceUrlCursor);
                            //calling communicator again for pagination data
                            var responseData = _twitterAPICommunicator.GetResponse(resourceUrlCursor, Method.GET, nextCursor);
                            log.Info($"TwitterDataCommunicator GetCampaignList: get the resourceUrlCursor response");
                            using (var sd = new StreamReader(responseData.GetResponseStream()))
                            {
                                dynamic resultCursor = JObject.Parse(sd.ReadToEnd());
                                nextCursor = Convert.ToString(resultCursor.next_cursor);
                                JArray resultCursorData = (JArray)resultCursor.SelectToken("data");
                                log.Info($"TwitterDataCommunicator GetCampaignList: Adding resourceUrlCursor response data to campaign entity");
                                //adding response data to list of campaigns
                                AddCampaignToList(listAllCampaigns, resultCursorData);
                                responseData.Close();
                            }

                        } while (!string.IsNullOrEmpty(nextCursor));
                            
                    }
                    response.Close();
                }
                
                log.Info($"TwitterDataCommunicator GetCampaignList: For delta load or full load filter the campaigns of current year");
                //for delta/full load get all the campigns of current year
                listFinalCampaigns.AddRange(listAllCampaigns.Where(x=> x.StartDate.Year == DateTime.Now.Year).Select(x => x).ToList());
                log.Info($"TwitterDataCommunicator GetCampaignList: Method end");
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetCampaignList: Exception Found "+ ex.Message);
                throw;
            }            
            return listFinalCampaigns;
        }
        /// <summary>
        /// Use this method to get list of line items based on campaigns id
        /// </summary>
        /// <param name="listCampaigns">list of campaigns</param>              
        /// <returns>Return the list of line items</returns>
        public List<TWLineItemEntity> GetLineItemsList(List<TWCampaignEntity> listCampaigns)
        {            
            List<TWLineItemEntity> listLineItems = new List<TWLineItemEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetLineItemsList: Method start");
                log.Info($"TwitterDataCommunicator GetLineItemsList: getting list of ids of campaigns from campaigns list");
                // getting list of ids of campaigns from campaigns list
                List<string> listCampaignIDs = listCampaigns.Select(x => x.CampaignID).ToList();
                if (listCampaignIDs != null && listCampaignIDs.Count > 0)
                {
                    log.Info($"TwitterDataCommunicator GetLineItemsList: ids count " + listCampaignIDs.Count);
                    int pageSize = Constant.pageSize;
                    // based on campaign ids get the total page count :  max 20 ids data we can get in single page call
                    int totalPageCount = (listCampaignIDs.Count % pageSize != 0) ? listCampaignIDs.Count / pageSize + 1 : listCampaignIDs.Count / pageSize;
                    //loop through till total page count
                    log.Info($"TwitterDataCommunicator GetLineItemsList: totalPageCount count " + totalPageCount);
                    for (int pageNumber = 1; pageNumber <= totalPageCount; pageNumber++)
                    {
                        //get the camapigns ids for single page request / max 20 ids
                        var listCampaignIDsForSinglePage = listCampaignIDs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                        //assemble the request url with all the required parameters
                        string apiURLParameters = String.Format(_applicationSettings.TWAdsLineItemDataURI, _applicationSettings.TWAccountID, string.Join(",", listCampaignIDsForSinglePage));
                        string resourceUrl = _applicationSettings.TWAdsAPIUrl + apiURLParameters;
                        //calling communicator class for response
                        log.Info($"TwitterDataCommunicator GetLineItemsList: resourceUrl " + resourceUrl);
                        var response = _twitterAPICommunicator.GetResponse(resourceUrl, Method.GET);
                        log.Info($"TwitterDataCommunicator GetLineItemsList: get the response for resourceUrl");
                        using (var sr = new StreamReader(response.GetResponseStream()))
                        {
                            dynamic result = JObject.Parse(sr.ReadToEnd());
                            JArray resultData = (JArray)result.SelectToken("data");
                            log.Info($"TwitterDataCommunicator GetLineItemsList: Adding response data to lineitems entity");
                            // add response data to list of line items
                            AddLineItemToList(listLineItems, resultData);

                            //check if pagination data exist for the request
                            if (!string.IsNullOrEmpty(Convert.ToString(result.next_cursor)))
                            {
                                log.Info($"TwitterDataCommunicator GetLineItemsList:  getting pagination data for the request");
                                string nextCursor = Convert.ToString(result.next_cursor);
                                do
                                {
                                    //asign the cursor value to request
                                    string resourceUrlCursor = resourceUrl + "&cursor=" + nextCursor;
                                    // calling communicator again for pagination data
                                    log.Info($"TwitterDataCommunicator GetLineItemsList: resourceUrlCursor " + resourceUrlCursor);
                                    var responseData = _twitterAPICommunicator.GetResponse(resourceUrlCursor, Method.GET, nextCursor);
                                    log.Info("TwitterDataCommunicator GetLineItemsList:  get the response for resourceUrlCursor");
                                    using (var sd = new StreamReader(responseData.GetResponseStream()))
                                    {
                                        dynamic resultCursor = JObject.Parse(sd.ReadToEnd());
                                        nextCursor = Convert.ToString(resultCursor.next_cursor);
                                        JArray resultCursorData = (JArray)resultCursor.SelectToken("data");
                                        log.Info($"TwitterDataCommunicator GetLineItemsList: Adding resourceUrlCursor response data to lineitems entity");
                                        //adding pagination response data to line item list
                                        AddLineItemToList(listLineItems, resultCursorData);
                                        responseData.Close();
                                    }

                                } while (!string.IsNullOrEmpty(nextCursor));
                            }
                            response.Close();
                        }
                    }
                }
                log.Info($"TwitterDataCommunicator GetLineItemsList: Method end");
                return listLineItems;
            }
            catch (Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetLineItemsList: Exception found "+ ex.Message);
                throw;
            }            
        }
        /// <summary>
        /// Use this method to get the list of Promoted tweets based on line item ids
        /// </summary>
        /// <param name="listLineItems">list of line items</param>                
        /// <returns>Return the list of promoted tweets</returns>
        public List<TWTweetsEntity> GetPromotedTweetsList(List<TWLineItemEntity> listLineItems)
        {            
            List<TWTweetsEntity> listPromotedTweets = new List<TWTweetsEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetPromotedTweetsList: Method start");
                log.Info($"TwitterDataCommunicator GetPromotedTweetsList: getting list of ids of lineItems from lineItems list");
                // getting list of ids of lineItems from lineItems list
                List<string> listLineItemsIDs = listLineItems.Select(x => x.LineItemID).ToList();
                if (listLineItemsIDs != null && listLineItemsIDs.Count > 0)
                {
                    log.Info($"TwitterDataCommunicator GetPromotedTweetsList: LineItemsIDs count " + listLineItemsIDs.Count);
                    int pageSize = Constant.pageSize;
                    // based on lineItems ids get the total page count :  max 20 ids data we can get in single page call
                    int totalPageCount = (listLineItemsIDs.Count % pageSize != 0) ? listLineItemsIDs.Count / pageSize + 1 : listLineItemsIDs.Count / pageSize;
                    log.Info($"TwitterDataCommunicator GetPromotedTweetsList: totalPageCount count " + totalPageCount);
                    //loop through till total page count
                    for (int pageNumber = 1; pageNumber <= totalPageCount; pageNumber++)
                    {
                        //get the lineItems ids for single page request / max 20 ids
                        var listLineItemsIDsForSinglePage = listLineItemsIDs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                        //assemble the request url with all the required parameters
                        string apiURLParameters = String.Format(_applicationSettings.TWAdsPromotedTweetsDataURI, _applicationSettings.TWAccountID, string.Join(",", listLineItemsIDsForSinglePage));
                        string resourceUrl = _applicationSettings.TWAdsAPIUrl + apiURLParameters;
                        log.Info($"TwitterDataCommunicator GetPromotedTweetsList: resourceUrl  " + resourceUrl);
                        //calling communicator class for response
                        var response = _twitterAPICommunicator.GetResponse(resourceUrl, Method.GET);
                        log.Info($"TwitterDataCommunicator GetPromotedTweetsList: get the response");
                        using (var sr = new StreamReader(response.GetResponseStream()))
                        {
                            dynamic result = JObject.Parse(sr.ReadToEnd());
                            JArray resultData = (JArray)result.SelectToken("data");
                            log.Info($"TwitterDataCommunicator GetPromotedTweetsList: adding response data to PromotedTweets entity");
                            // add response data to list of promoted tweets
                            AddPromotedTweetsToList(listPromotedTweets, resultData);
                            //check if pagination data exist for the request
                            if (!string.IsNullOrEmpty(Convert.ToString(result.next_cursor)))
                            {
                                log.Info($"TwitterDataCommunicator GetPromotedTweetsList: getting pagination data for the request");
                                string nextCursor = Convert.ToString(result.next_cursor);
                                do
                                {
                                    string resourceUrlCursor = resourceUrl + "&cursor=" + nextCursor;
                                    log.Info($"TwitterDataCommunicator GetPromotedTweetsList: resourceUrlCursor "+ resourceUrlCursor);
                                    //calling communicator class again for pagination response
                                    var responseData = _twitterAPICommunicator.GetResponse(resourceUrlCursor, Method.GET, nextCursor);
                                    log.Info($"TwitterDataCommunicator GetPromotedTweetsList: get the response for resourceUrlCursor");
                                    using (var sd = new StreamReader(responseData.GetResponseStream()))
                                    {
                                        dynamic resultCursor = JObject.Parse(sd.ReadToEnd());
                                        nextCursor = Convert.ToString(resultCursor.next_cursor);
                                        JArray resultCursorData = (JArray)resultCursor.SelectToken("data");
                                        log.Info($"TwitterDataCommunicator GetPromotedTweetsList: adding resourceUrlCursor response data to PromotedTweets entity");
                                        // add response data to list of promoted tweets
                                        AddPromotedTweetsToList(listPromotedTweets, resultCursorData);
                                        responseData.Close();
                                    }

                                } while (!string.IsNullOrEmpty(nextCursor));

                            }
                            response.Close();
                        }
                    }
                }
                log.Info($"TwitterDataCommunicator GetPromotedTweetsList: Method end");
                return listPromotedTweets;
            }
            catch (Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetPromotedTweetsList: Exception found "+ ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to get the list of tweets based on promoted tweets ids
        /// </summary>
        /// <param name="listPromotedTweets">list of promoted tweet</param>            
        /// <returns>Return the list of tweets</returns>
        public List<TWTweetsEntity> GetTweetsList(List<TWTweetsEntity> listPromotedTweets)
        {            
            List<TWTweetsEntity> listTweets = new List<TWTweetsEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetTweetsList: Method start");
                log.Info($"TwitterDataCommunicator GetTweetsList: getting list of ids of promoted tweets from promoted tweets list");
                // getting list of ids of promoted tweets from promoted tweets list
                List<string> listTweetsIDs = listPromotedTweets.Select(x => x.TweetID).ToList();
                if (listTweetsIDs != null && listTweetsIDs.Count > 0)
                {
                    log.Info($"TwitterDataCommunicator GetTweetsList: TweetsIDs count "+ listTweetsIDs.Count);
                    int pageSize = Constant.pageSize;
                    // based on promoted tweets ids get the total page count :  max 20 ids data we can get in single page call
                    int totalPageCount = (listTweetsIDs.Count % pageSize != 0) ? listTweetsIDs.Count / pageSize + 1 : listTweetsIDs.Count / pageSize;
                    log.Info($"TwitterDataCommunicator GetTweetsList: totalPageCount count " + totalPageCount);
                    //loop through till total page count
                    for (int pageNumber = 1; pageNumber <= totalPageCount; pageNumber++)
                    {
                        //get the promoted tweets ids for single page request / max 20 ids
                        var listTweetsIDsForSinglePage = listTweetsIDs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                        //assemble the request url with all the required parameters
                        string apiURLParameters = String.Format(_applicationSettings.TWAdsTweetsDataURI, _applicationSettings.TWAccountID, string.Join(",", listTweetsIDsForSinglePage));
                        string resourceUrl = _applicationSettings.TWAdsAPIUrl + apiURLParameters;
                        log.Info($"TwitterDataCommunicator GetTweetsList: resourceUrl " + resourceUrl);
                        //calling communicator class for response
                        var response = _twitterAPICommunicator.GetResponse(resourceUrl, Method.GET);
                        log.Info($"TwitterDataCommunicator GetTweetsList: get the response ");
                        using (var sr = new StreamReader(response.GetResponseStream()))
                        {
                            dynamic result = JObject.Parse(sr.ReadToEnd());
                            JArray resultData = (JArray)result.SelectToken("data");
                            log.Info($"TwitterDataCommunicator GetTweetsList: Adding response data to tweets entity");
                            // add response data to list of tweets
                            AddTweetsToList(listTweets, resultData, listPromotedTweets);
                            //check if pagination data exist for the request
                            if (!string.IsNullOrEmpty(Convert.ToString(result.next_cursor)))
                            {
                                log.Info($"TwitterDataCommunicator GetTweetsList: get the pagination data for the request ");
                                string nextCursor = Convert.ToString(result.next_cursor);
                                do
                                {
                                    string resourceUrlCursor = resourceUrl + "&cursor=" + nextCursor;
                                    log.Info($"TwitterDataCommunicator GetTweetsList: resourceUrlCursor " + resourceUrlCursor);
                                    //calling communicator class again for pagination response data
                                    var responseData = _twitterAPICommunicator.GetResponse(resourceUrlCursor, Method.GET, nextCursor);
                                    log.Info($"TwitterDataCommunicator GetTweetsList: get the response for resourceUrlCursor");
                                    using (var sd = new StreamReader(responseData.GetResponseStream()))
                                    {
                                        dynamic resultCursor = JObject.Parse(sd.ReadToEnd());
                                        nextCursor = Convert.ToString(resultCursor.next_cursor);
                                        JArray resultCursorData = (JArray)resultCursor.SelectToken("data");
                                        log.Info($"TwitterDataCommunicator GetTweetsList: Adding resourceUrlCursor response data to tweets entity");
                                        // add response data to list of tweets
                                        AddTweetsToList(listTweets, resultCursorData, listPromotedTweets);
                                        responseData.Close();
                                    }

                                } while (!string.IsNullOrEmpty(nextCursor));
                            }
                            response.Close();
                        }
                    }
                }
                log.Info($"TwitterDataCommunicator GetTweetsList: Method end");
                return listTweets;
            }
            catch (Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetTweetsList: Exception Found "+ ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to list of Twitter Ads data
        /// </summary>
        /// <param name="placement">ALLOnTwitter/Publisher_Network</param>
        /// <param name="lastRunDate">date of last run</param>
        /// <param name="requestModel">requestModel</param>
        /// <param name="listPromotedTweets">list of promoted tweets</param>
        /// <param name="listTweets">list of tweets</param>
        /// <returns>Return the list of FB ads data</returns>
        public List<TWAdsEntity> GetAdsMasterDataList(string placement, string lastRunDate = null, RequestModel requestModel = null, List<TWTweetsEntity> listPromotedTweets = null, List<TWTweetsEntity> listTweets = null)
        {            
            List<TWAdsEntity> listAdsEntity = new List<TWAdsEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetAdsMasterDataList: Method start");
                log.Info($"TwitterDataCommunicator GetAdsMasterDataList: getting list of ids of promoted tweets from promoted tweets list");
                // getting list of ids of promoted tweets from promoted tweets list
                List<string> promotedTweetsIdList = listPromotedTweets != null && listPromotedTweets.Count > 0 ? listPromotedTweets.Select(x => x.PromotedTweetID).ToList() : null;
                if (promotedTweetsIdList != null && promotedTweetsIdList.Count > 0)
                {
                    log.Info($"TwitterDataCommunicator GetAdsMasterDataList: promotedTweetsId count "+ promotedTweetsIdList.Count);
                    int.TryParse(_applicationSettings.TWDayChunkValue, out int twDayChunkValue);
                    bool.TryParse(_applicationSettings.TWFullLoadForMonth, out bool twFullLoadForMonth);
                    requestModel.FullLoadForMonth = twFullLoadForMonth;
                    log.Info($"TwitterDataCommunicator GetAdsMasterDataList: getting date range list for twDayChunkValue " + twDayChunkValue);
                    //getting date range list for delta and full load
                    var datesList = _twitterServices.GetBusinessDatesList(lastRunDate, requestModel, twDayChunkValue, Constant.twitter);
                    //setting plateform Placement Values for Request and Report
                    string platformPlacementForRequest = placement == Constant.TwPlacementAll ? Constant.TwPlacementAll : Constant.TwPlacementPublisher;
                    string platformPlacementForReport = placement == Constant.TwPlacementAll ? _applicationSettings.TWPlacementTwitter : _applicationSettings.TWPlacementTwitterAudience;
                    log.Info($"TwitterDataCommunicator GetAdsMasterDataList: loop through till total page count for platformPlacement " + platformPlacementForRequest);
                    GetDataForAllPages(ref listAdsEntity, datesList, promotedTweetsIdList, platformPlacementForRequest, platformPlacementForReport, listTweets);
                }
            }
            catch (Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetAdsMasterDataList: Exception Found "+ ex.Message);
                throw;
            }            
            return listAdsEntity;
        }

        /// <summary>
        /// Use this method loop through date list and get all the data
        /// </summary>
        /// <param name="listAdsEntity">list of entity</param>
        /// <param name="datesList">date for which data needed</param> 
        /// <param name="promotedTweetsIdList">list of promotedTweetsIds</param>
        /// <param name="platformPlacementForRequest">platformPlacementForRequest</param>
        /// <param name="platformPlacementForReport">platformPlacementForReport</param>
        /// <param name="listTweets">list of tweets</param>        
        /// <returns>void</returns>
        private void GetDataForAllPages(ref List<TWAdsEntity> listAdsEntity, List<YearsMonths> datesList, List<string> promotedTweetsIdList, string platformPlacementForRequest, string platformPlacementForReport, List<TWTweetsEntity> listTweets = null)
        {
            try
            {
                //loop through till total page count
                foreach (YearsMonths dates in datesList)
                {
                    log.Info($"TwitterDataCommunicator GetDataForAllPages: for date range start date: " + dates.StartDate + "And End date: " + dates.EndDate);
                    int pageSize = Constant.pageSize;
                    // based on promoted tweets ids get the total page count :  max 20 ids data we can get in single page call
                    int totalPageCount = (promotedTweetsIdList.Count % pageSize != 0) ? promotedTweetsIdList.Count / pageSize + 1 : promotedTweetsIdList.Count / pageSize;
                    log.Info($"TwitterDataCommunicator GetDataForAllPages: totalPageCount " + totalPageCount);
                    for (int pageNumber = 1; pageNumber <= totalPageCount; pageNumber++)
                    {
                        //get the promoted tweets ids for single page request / max 20 ids
                        var listPromotedTweetsIDsForSinglePage = promotedTweetsIdList.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                        //assemble the request url with all the required parameters
                        string apiURLParameters = String.Format(_applicationSettings.TWAdsSynchronousDataURI, _applicationSettings.TWAccountID, dates.EndDate.ToString(), string.Join(",", listPromotedTweetsIDsForSinglePage), Constant.granularityDay, platformPlacementForRequest, dates.StartDate.ToString());
                        string resourceUrl = _applicationSettings.TWAdsAPIUrl + apiURLParameters;
                        log.Info($"TwitterDataCommunicator GetDataForAllPages: resourceUrl " + resourceUrl);
                        //calling communicator class for response
                        var response = _twitterAPICommunicator.GetResponse(resourceUrl, Method.GET);
                        log.Info($"TwitterDataCommunicator GetDataForAllPages: get the response");
                        using (var sr = new StreamReader(response.GetResponseStream()))
                        {
                            dynamic result = JObject.Parse(sr.ReadToEnd());
                            JArray resultData = (JArray)result.SelectToken("data");
                            log.Info($"TwitterDataCommunicator GetDataForAllPages: Adding response data to ads entity");
                            // add response data to list of ads
                            AddTwitterAdsDataToList(listAdsEntity, resultData, platformPlacementForReport, dates, listTweets);
                            //check if pagination data exist for the request
                            if (!string.IsNullOrEmpty(Convert.ToString(result.next_cursor)))
                            {
                                log.Info($"TwitterDataCommunicator GetDataForAllPages: get the pagination data");
                                string nextCursor = Convert.ToString(result.next_cursor);
                                do
                                {
                                    string resourceUrlCursor = resourceUrl + "?cursor=" + nextCursor;
                                    log.Info($"TwitterDataCommunicator GetDataForAllPages: resourceUrlCursor " + resourceUrlCursor);
                                    //calling communicator class again for pagination response
                                    var responseData = _twitterAPICommunicator.GetResponse(resourceUrlCursor, Method.GET, nextCursor);
                                    log.Info($"TwitterDataCommunicator GetDataForAllPages: get the response for resourceUrlCursor");
                                    using (var sd = new StreamReader(responseData.GetResponseStream()))
                                    {
                                        dynamic resultCursor = JObject.Parse(sd.ReadToEnd());
                                        nextCursor = Convert.ToString(resultCursor.next_cursor);
                                        JArray resultCursorData = (JArray)resultCursor.SelectToken("data");
                                        log.Info($"TwitterDataCommunicator GetDataForAllPages: Adding resourceUrlCursor response data to ads entity");
                                        // add response data to list of ads
                                        AddTwitterAdsDataToList(listAdsEntity, resultCursorData, platformPlacementForReport, dates, listTweets);
                                        responseData.Close();
                                    }

                                } while (!string.IsNullOrEmpty(nextCursor));

                            }
                            response.Close();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetDataForAllPages: Exception Found " + ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to add the Jarray data to campains list
        /// </summary>
        /// <param name="listAllCampaigns">list of campaign where to add</param>
        /// <param name="resultData">respons data of campaigns</param>        
        /// <returns>Add the campains to list</returns>
        private void AddCampaignToList(List<TWCampaignEntity> listAllCampaigns, JArray resultData)
        {            
            try
            {
                log.Info($"TwitterDataCommunicator AddCampaignToList: Method start");
                //adding response data to list of campaigns
                foreach (JToken item in resultData)
                {
                    TWCampaignEntity entity = new TWCampaignEntity
                    {
                        CampaignName = Convert.ToString(item.SelectToken("name")),
                        CampaignID = Convert.ToString(item.SelectToken("id")),
                        Status = Convert.ToString(item.SelectToken("entity_status"))
                    };
                    if (!string.IsNullOrEmpty(Convert.ToString(item.SelectToken("start_time"))) && !string.IsNullOrEmpty(Convert.ToString(item.SelectToken("end_time"))))
                    {
                        entity.StartDate=  DateTime.Parse(item.SelectToken("start_time").ToString());
                        entity.EndDate = DateTime.Parse(item.SelectToken("end_time").ToString());
                        listAllCampaigns.Add(entity);
                    }
                }
                log.Info($"TwitterDataCommunicator AddCampaignToList: Method end");
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator AddCampaignToList: Exception Found "+ ex.Message);
                throw;
            }            
        }
        /// <summary>
        /// Use this method to add the Jarray data to line item list
        /// </summary>
        /// <param name="listLineItems">list of lineitems where to add</param>
        /// <param name="resultData">respons data of lineitems</param>        
        /// <returns>Add the lineitems to list</returns>
        private void AddLineItemToList(List<TWLineItemEntity> listLineItems, JArray resultData)
        {            
            try
            {
                log.Info($"TwitterDataCommunicator AddLineItemToList: Method start");
                //adding response data to list of line items
                foreach (JToken item in resultData)
                {
                    TWLineItemEntity entity = new TWLineItemEntity
                    {
                        CampaignID = Convert.ToString(item.SelectToken("campaign_id")),
                        LineItemID = Convert.ToString(item.SelectToken("id")),
                        LineItemName = Convert.ToString(item.SelectToken("name")),
                        AdvertiserUserID = Convert.ToString(item.SelectToken("advertiser_user_id")),
                        LineItemStatus = Convert.ToString(item.SelectToken("entity_status"))
                    };
                    listLineItems.Add(entity);
                }
                log.Info($"TwitterDataCommunicator AddLineItemToList: Method end");
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator AddLineItemToList: Exception Found "+ ex.Message);
                throw;
            }            
        }
        /// <summary>
        /// Use this method to add the Jarray data to promoted tweets list
        /// </summary>
        /// <param name="listPromotedTweets">list of promoted tweets where to add</param>
        /// <param name="resultData">respons data of promoted tweets</param>        
        /// <returns>Add the promoted tweets to list</returns>
        private void AddPromotedTweetsToList(List<TWTweetsEntity> listPromotedTweets, JArray resultData)
        {            
            try
            {
                log.Info($"TwitterDataCommunicator AddPromotedTweetsToList: Method start");
                //adding response data to list of promoted tweets
                foreach (JToken item in resultData)
                {
                    TWTweetsEntity entity = new TWTweetsEntity
                    {
                        LineItemID = Convert.ToString(item.SelectToken("line_item_id")),
                        PromotedTweetID = Convert.ToString(item.SelectToken("id")),
                        TweetID = Convert.ToString(item.SelectToken("tweet_id")),
                        TweetStatus = Convert.ToString(item.SelectToken("entity_status"))
                    };
                    listPromotedTweets.Add(entity);
                }
                log.Info($"TwitterDataCommunicator AddPromotedTweetsToList: Method end");
            }
            catch (Exception ex)
            {
                log.Info($"TwitterDataCommunicator AddPromotedTweetsToList: Exception Found "+ ex.Message);
                throw;
            }            
        }
        /// <summary>
        /// Use this method to add the Jarray data to tweets list
        /// </summary>
        /// <param name="listTweets">list of tweets where to add</param>
        /// <param name="listPromotedTweets">list of promoted tweets</param>
        /// <param name="resultData">respons data of tweets</param>        
        /// <returns>Add the tweets to list</returns>
        private void AddTweetsToList(List<TWTweetsEntity> listTweets, JArray resultData, List<TWTweetsEntity> listPromotedTweets)
        {            
            try
            {
                log.Info($"TwitterDataCommunicator AddTweetsToList: Method start");
                //adding response data to list of tweets
                foreach (JToken item in resultData)
                {
                    TWTweetsEntity entity = new TWTweetsEntity
                    {
                        PromotedTweetID = listPromotedTweets.Where(x=> x.TweetID == Convert.ToString(item.SelectToken("tweet_id"))).Select(x=> x.PromotedTweetID).FirstOrDefault(),
                        TweetID = Convert.ToString(item.SelectToken("tweet_id")),
                        WebsiteURL = Convert.ToString(item.SelectToken("entities.urls[0].expanded_url"))
                    };
                    listTweets.Add(entity);
                }
                log.Info($"TwitterDataCommunicator AddTweetsToList: Method end");
            }
            catch (Exception ex)
            {
                log.Info($"TwitterDataCommunicator AddTweetsToList: Exception Found "+ ex.Message);
                throw;
            }            
        }
        /// <summary>
        /// Use this method to add the Jarray data to twitter ads list
        /// </summary>
        /// <param name="listAdsEntity">list of twitter ads where to add</param>
        /// <param name="platform">ALLOnTwitter/ Publisher_Platform</param>
        /// <param name="dates">dateRange</param>
        /// <param name="listTweets">list of tweets where to add</param>
        /// <param name="resultData">respons data of twitter ads</param>        
        /// <returns>Add the twitter ads to list</returns>
        private void AddTwitterAdsDataToList(List<TWAdsEntity> listAdsEntity, JArray resultData, string platform, YearsMonths dates, List<TWTweetsEntity> listTweets)
        {            
            try
            {
                log.Info($"TwitterDataCommunicator AddTwitterAdsDataToList: Method start");
                //adding response data to list of ads
                foreach (JToken item in resultData)
                {                    
                    DateTime dtEnd = Convert.ToDateTime(dates.EndDate);
                    string[] impressionsArray = item.SelectToken("id_data[0].metrics.impressions").ToObject<string[]>();
                    string[] spendArray = item.SelectToken("id_data[0].metrics.billed_charge_local_micro").ToObject<string[]>();
                    string[] postEngagementArray = item.SelectToken("id_data[0].metrics.engagements").ToObject<string[]>();
                    string[] clicksAllArray = item.SelectToken("id_data[0].metrics.clicks").ToObject<string[]>();
                    string[] linkClicksArray = item.SelectToken("id_data[0].metrics.url_clicks").ToObject<string[]>();
                    string[] siteVisitsArray = item.SelectToken("id_data[0].metrics.conversion_site_visits.metric").ToObject<string[]>();
                    string[] videoCompletionsArray = item.SelectToken("id_data[0].metrics.video_total_views").ToObject<string[]>();
                    int i = 0;
                    for (DateTime dtStart = Convert.ToDateTime(dates.StartDate); dtStart < dtEnd; dtStart = dtStart.AddDays(1))
                    {
                        TWAdsEntity adEntity = new TWAdsEntity
                        {
                            PromotedTweetID = item.SelectToken("id").ToString(),
                            Impressions = impressionsArray != null && impressionsArray.Count() > i ? impressionsArray[i] : null,
                            Spend = spendArray != null && spendArray.Count() > i ? spendArray[i] : null,
                            Post_Engagement = postEngagementArray != null && postEngagementArray.Count() > i ? postEngagementArray[i] : null,
                            Clicks_All = clicksAllArray != null && clicksAllArray.Count() > i ? clicksAllArray[i] : null,
                            Link_Clicks = linkClicksArray != null && linkClicksArray.Count() > i ? linkClicksArray[i] : null,
                            Link_Clicks_Unique = linkClicksArray != null && linkClicksArray.Count() > i ? linkClicksArray[i] : null,
                            Site_Visits = siteVisitsArray != null && siteVisitsArray.Count() > i ? siteVisitsArray[i] : null,
                            Video_Completions = videoCompletionsArray != null && videoCompletionsArray.Count() > i ? videoCompletionsArray[i] : null,
                            Platform_Placement = platform,
                            Created_Date = dtStart,
                            WebsiteURL = listTweets
                                            .Where(x=> x.PromotedTweetID == item.SelectToken("id").ToString())                                            
                                            .Select(s=> s.WebsiteURL).FirstOrDefault()
                        };
                        listAdsEntity.Add(adEntity);
                        i++;
                        
                    }
                }
                log.Info($"TwitterDataCommunicator AddTwitterAdsDataToList: Method end");
            }
            catch (Exception ex)
            {
                log.Info($"TwitterDataCommunicator AddTwitterAdsDataToList: Exception Found "+ ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Use this method to make the final list with combining all the related data list
        /// </summary>
        /// <param name="listCampaigns">list of campaigns</param>
        /// <param name="listLineItems">list of lineitems</param>
        /// <param name="listPromotedTweets">list of promoted tweets</param>
        /// <param name="adsData_ALL_ON_TWITTER">list of ALLOnTwitters ads data</param>
        /// <param name="adsData_PUBLISHER_NETWORK">list of Publisher_Networks data</param>            
        /// <returns>return final twitter ads data</returns>
        public List<TWFinalAdsEntity> GetFinalTwitterAdsDataList(List<TWCampaignEntity> listCampaigns, List<TWLineItemEntity> listLineItems, List<TWTweetsEntity> listPromotedTweets, List<TWAdsEntity> adsData_ALL_ON_TWITTER, List<TWAdsEntity> adsData_PUBLISHER_NETWORK)
        {            
            List<TWFinalAdsEntity> finalAdsList = new List<TWFinalAdsEntity>();
            try
            {
                log.Info($"TwitterDataCommunicator GetFinalTwitterAdsDataList: Method start");
                //combining all the data to final ads list
                int rowNo = 0;
                foreach (var campaign in listCampaigns)
                {
                    List<TWLineItemEntity> filteredLineItemList = listLineItems.Where(x => x.CampaignID == campaign.CampaignID).Select(x => x).ToList();
                    foreach (var lineItem in filteredLineItemList)
                    {
                        List<TWTweetsEntity> filteredPromotedTweetsList = listPromotedTweets.Where(x => x.LineItemID == lineItem.LineItemID).Select(x => x).ToList();
                        foreach (var promotedTweet in filteredPromotedTweetsList)
                        {
                            List<TWAdsEntity> filteredAdsAllONTwitter = adsData_ALL_ON_TWITTER.Where(x => x.PromotedTweetID == promotedTweet.PromotedTweetID).Select(x => x).ToList();
                            AddTwitterPublisherData(campaign, lineItem, promotedTweet, filteredAdsAllONTwitter, ref finalAdsList, ref rowNo);
                            List<TWAdsEntity> filteredAdsPublisher = adsData_PUBLISHER_NETWORK.Where(x => x.PromotedTweetID == promotedTweet.PromotedTweetID).Select(x => x).ToList();
                            AddTwitterPublisherData(campaign, lineItem, promotedTweet, filteredAdsPublisher, ref finalAdsList, ref rowNo);
                        }
                    }
                }
                log.Info($"TwitterDataCommunicator GetFinalTwitterAdsDataList: Method end");
                return finalAdsList;
            }
            catch (Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetFinalTwitterAdsDataList: Exception found "+ ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Add ad item in final list
        /// </summary>
        /// <param name="campaign">list of campaigns</param>
        /// <param name="lineItem">list of lineitems</param>
        /// <param name="promotedTweet">list of promoted tweets</param>
        /// <param name="filteredAdsList">list of ALLOnTwitters/Publisher_Networks ads data</param>
        /// <param name="rowNo">rowNo</param>            
        /// <returns>void</returns>
        private void AddTwitterPublisherData(TWCampaignEntity campaign, TWLineItemEntity lineItem, TWTweetsEntity promotedTweet, List<TWAdsEntity> filteredAdsList, ref List<TWFinalAdsEntity> finalAdsList, ref int rowNo)
        {
            try
            {   
                List<TWAdsEntity> filteredAds = filteredAdsList.Where(x => x.PromotedTweetID == promotedTweet.PromotedTweetID).Select(x => x).ToList();
                foreach (var ads in filteredAds)
                {
                    TWFinalAdsEntity adEntity = new TWFinalAdsEntity
                    {
                        FileRowNo = ++rowNo,
                        AccountID = _applicationSettings.TWAccountID,
                        AccountName = _applicationSettings.TWAccountName,
                        CampaignID = campaign.CampaignID,
                        CampaignName = campaign.CampaignName,
                        AdSetID = lineItem.AdvertiserUserID,
                        AdSetName = lineItem.LineItemName.LastIndexOf("_") > 0 ? lineItem.LineItemName.Substring(0, lineItem.LineItemName.LastIndexOf('_')) : lineItem.LineItemName,
                        AdID = promotedTweet.TweetID,
                        AdName = lineItem.LineItemName,
                        Impressions = ads.Impressions,
                        Post_Engagement = ads.Post_Engagement,
                        Clicks_All = ads.Clicks_All,
                        Link_Clicks = ads.Link_Clicks,
                        Link_Clicks_Unique = ads.Link_Clicks_Unique,
                        Site_Visits = ads.Site_Visits,
                        Video_Completions = ads.Video_Completions,
                        Date_Start = ads.Created_Date.ToString("yyyy-MM-dd"),
                        Date_Stop = ads.Created_Date.ToString("yyyy-MM-dd"),
                        Platform_Placement = ads.Platform_Placement,
                        WebsiteURL = ads.WebsiteURL
                    };
                    ads.Spend = !string.IsNullOrEmpty(ads.Spend) ? Convert.ToString((ads.Spend.Split(new[] { ',' }).Select(x => double.Parse(x.Trim())).Sum() / 1000000)) : "";
                    adEntity.Spend = ads.Spend;
                    double.TryParse(ads.Spend, out double spend);
                    double.TryParse(ads.Video_Completions, out double videoCompletion);
                    adEntity.Cost_per_video_view = spend > 0 && videoCompletion > 0 ? Convert.ToString(spend / videoCompletion) : null;

                    finalAdsList.Add(adEntity);
                }
            }
            catch(Exception ex)
            {
                log.Error($"TwitterDataCommunicator GetFinalTwitterAdsDataList: Exception found " + ex.Message);
                throw;
            }
        }
    }
}
