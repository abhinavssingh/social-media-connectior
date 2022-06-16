using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataConnector.Intg.SocialMedia.Entities
{

    public class AdEntity
    {
        [JsonProperty("account_id")]
        public string AccountID { get; set; }

        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("campaign_id")]
        public string CapaignID { get; set; }

        [JsonProperty("campaign_name")]
        public string CampaignName { get; set; }

        [JsonProperty("ad_id")]
        public string AdID { get; set; }

        [JsonProperty("ad_name")]
        public string AdName { get; set; }

        [JsonProperty("adset_id")]
        public string AdSetID { get; set; }

        [JsonProperty("adset_name")]
        public string AdSetName { get; set; }

        [JsonProperty("spend")]
        public string Spend { get; set; }

        [JsonProperty("impressions")]
        public string Impressions { get; set; }

        [JsonProperty("inline_post_engagement")]
        public string Post_Engagement { get; set; }

        [JsonProperty("clicks")]
        public string Clicks_All { get; set; }

        [JsonProperty("inline_link_clicks")]
        public string Link_Clicks { get; set; }

        [JsonProperty("unique_inline_link_clicks")]
        public string Link_Clicks_Unique { get; set; }
        [JsonProperty("cost_per_thruplay")]
        public List<AdsActionStatsEntity> Cost_Per_ThruPlay { get; set; }

        [JsonProperty("video_thruplay_watched_actions")]
        public List<AdsActionStatsEntity> Video_ThruPlay_Watched_Actions { get; set; }

        [JsonProperty("date_start")]
        public string Date_Start { get; set; }

        [JsonProperty("date_stop")]
        public string Date_Stop { get; set; }

        [JsonProperty("publisher_platform")]
        public string Platform { get; set; }

        [JsonProperty("platform_position")]
        public string Platform_Placement { get; set; }

        [JsonProperty("link_url_asset")]
        public LinkURLAsset Link_URL_Asset { get; set; }

        public string WebsiteURL { get; set; }
    }

    /// <summary>
    /// AdsLists - for Account
    /// </summary>
    public class RootObjectAccount
    {
        [JsonProperty("data")]
        public List<AdEntity> AdsLists { get; set; }
    }

    /// <summary>
    /// AdsListsData - for Ads
    /// </summary>

    public class RootObjectAds
    {
        [JsonProperty("data")]
        public List<AdEntity> AdsDataList { get; set; }

        [JsonProperty("paging")]
        public Paging Paging { get; set; }
    }

    /// <summary>
    /// Nested object Ads Data
    /// </summary>
    public class LinkURLAsset
    {
        [JsonProperty("website_url")]
        public string Website_URL { get; set; }

        [JsonProperty("display_url")]
        public string Display_URL { get; set; }

        [JsonProperty("id")]
        public string Ad_ID { get; set; }

    }

    public class Paging
    {
        [JsonProperty("next")]
        public string NextURI { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }
    }
}
