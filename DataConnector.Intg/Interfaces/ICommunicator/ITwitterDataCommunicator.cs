using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface ITwitterDataCommunicator
    {        
        List<TWCampaignEntity> GetCampaignList(string lastRunDate = null);
        List<TWLineItemEntity> GetLineItemsList(List<TWCampaignEntity> listCampaigns);
        List<TWTweetsEntity> GetPromotedTweetsList(List<TWLineItemEntity> listLineItems);
        List<TWTweetsEntity> GetTweetsList(List<TWTweetsEntity> listPromotedTweets);
        List<TWAdsEntity> GetAdsMasterDataList(string placement, string lastRunDate = null, RequestModel requestModel = null, List<TWTweetsEntity> listPromotedTweets = null, List<TWTweetsEntity> listTweets = null);
        List<TWFinalAdsEntity> GetFinalTwitterAdsDataList(List<TWCampaignEntity> listCampaigns, List<TWLineItemEntity> listLineItems, List<TWTweetsEntity> listPromotedTweets, List<TWAdsEntity> adsData_ALL_ON_TWITTER, List<TWAdsEntity> adsData_PUBLISHER_NETWORK);
        List<TWFinalAdsEntity> GetTwitterAdsData(List<TWCampaignEntity> listCampaigns, List<TWLineItemEntity> listLineItems, string dtLastRun, RequestModel requestModel);
    }
}
