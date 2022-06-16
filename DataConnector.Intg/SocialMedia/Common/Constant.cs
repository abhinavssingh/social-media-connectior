namespace DataConnector.Intg.SocialMedia.Common
{
    public static class Constant
    {     
        public static readonly string accessTokenFormat = "&access_token=";
        public static readonly string accessTokenFormatForAdsCreative = "?access_token=";
        public static readonly string accountIdFormat = "act_";
        public static readonly string fbBreakdown_platform_platform_position = "publisher_platform, platform_position";
        public static readonly string fbBreakdown_WebsiteURL = "link_url_asset";
        public static readonly string FBAccessToken = "FBAccessToken";
        public static readonly string limit = "&limit=2000";
        public static readonly string GAServiceAccount = "GAServiceAccount";
        public const string OauthAPIVersion = "1.0";
        public const string OauthSignatureMethodType = "HMAC-SHA1";        
        public static readonly string oauthConsumerKey = "oauth_consumer_key";
        public static readonly string oauthNonce = "oauth_nonce";
        public static readonly string oauthSignatureMethod = "oauth_signature_method";
        public static readonly string oauthTimestamp = "oauth_timestamp";
        public static readonly string oauthToken = "oauth_token";
        public static readonly string oauthVersion = "oauth_version";
        public static readonly string oauthSignature = "oauth_signature";
        public static readonly string TwPlacementAll = "ALL_ON_TWITTER";
        public static readonly string TwPlacementPublisher = "PUBLISHER_NETWORK";
        public static readonly string granularityDay = "DAY";
        public static readonly string granularityTotal = "TOTAL";
        public static readonly int pageSize = 20;
        public static readonly string facebook = "Facebook";
        public static readonly string google = "Google";
        public static readonly string twitter = "Twitter";
        public static readonly string TWAccessToken = "TWAccessToken";
        public static readonly string TWAccessTokenSecret = "TWAccessTokenSecret";
        public static readonly string TWConsumerKey = "TWConsumerKey";
        public static readonly string TWConsumerKeySecret = "TWConsumerKeySecret";

        public static readonly string gaPageDimensions = "Date,Country,channelGrouping,Hostname,PagePath,PageTitle,socialNetwork,adContent,campaign";
        public static readonly string gaPageMetric = "Sessions|Sessions,PageViews|Page Views,UniquePageViews|Unique Page Views,Entrances|Entrances,Bounces|Bounces";
        public static readonly string gaEventDimensions = "Date,Country,channelGrouping,Hostname,eventCategory,eventAction,eventLabel,socialNetwork";
        public static readonly string gaEventMetric = "Sessions|Sessions,uniqueEvents|Unique Events,totalEvents|Total Events,sessionsWithEvent|Sessions With Event";
        public static readonly string gaGeoDimensions = "Date,Country,channelGrouping,Hostname,city,region,socialNetwork";
        public static readonly string gaGeoMetric = "Sessions|Sessions,UniquePageViews|Unique Page Views";
        public static readonly string gaPageEventDimensions = "Date,Country,channelGrouping,Hostname,eventCategory,eventAction,eventLabel,socialNetwork,PagePath";
        public static readonly string gaPageEventMetric = "Sessions|Sessions,uniqueEvents|Unique Events,totalEvents|Total Events,sessionsWithEvent|Sessions With Event";
        public static readonly string gaCustomDimensions = "Date,Country,channelGrouping,Hostname,socialNetwork";
        public static readonly string gaCustomMetric = "metric1|HelpMeChooseComplete,metric2|FindAStoreInteraction,metric3|ProductPageViewTireDetails";


        public static readonly string dv360 = "DV360";
        public static readonly string dv360ReportKind = "doubleclickbidmanager#query";

        public static readonly string dvCustomReportTitle = "DVCampaignDataCustom";
        public static readonly string dvTodayReportTitle = "DVCampaignDataToday";
        public static readonly string dvPreviousYearReportTitle = "DVCampaignDataPreviousYear";
        public static readonly string dvYearToDateReportTitle = "DVCampaignDataYearToDate";

        public static readonly string dvCustomDateRangeCustom = "CUSTOM_DATES";
        public static readonly string dvTodayDateRange = "CURRENT_DAY";
        public static readonly string dvPreviousYearDateRange = "PREVIOUS_YEAR";
        public static readonly string dvYearToDateDateRange = "YEAR_TO_DATE";
        
        public static readonly string dvCustomReportFrequency = "ONE_TIME";
        public static readonly string dvTodayReportFrequency = "ONE_TIME";
        public static readonly string dvPreviousYearReportFrequency = "ONE_TIME";
        public static readonly string dvYearToDateReportFrequency = "ONE_TIME";

        public static readonly string dvReportFormat = "CSV";
        public static readonly string dvApplicationName = "DV360 API Data";

        public static readonly string swSearchCountry = "us";
        public static readonly string swDesktopGranularity = "daily";
        public static readonly string swMobileGranularity = "monthly";
        public static readonly string swDesktopPlatform = "Desktop";
        public static readonly string swMobilePlatform = "Mobile";
        public static readonly string swApiKey = "SWAPIKey";

        public static readonly string gaTirePageDimensions = "Date,Country,Hostname";
        public static readonly string gaTirePageMetric = "UniquePageViews|Unique Page Views";
        public static readonly string gaTireEventDimensions = "Date,Country,Hostname,channelGrouping,PagePath,eventCategory,eventAction,eventLabel";
        public static readonly string gaTireEventMetric = "uniqueEvents|Unique Events";
        public static readonly string gaTireCustomDimensions = "Date,Country,Hostname,channelGrouping,dimension16";
        public static readonly string gaTireCustomMetric = "UniquePageViews|Unique Page Views";

        public static readonly string utcDateFormat = "yyyy-MM-dd";
        public static readonly string datasetDateFormat = "yyyyMMdd";
        public static readonly string sessionFileText = "Session";
        public static readonly string csvFileExtention = ".csv";
        public static readonly string xslxFileExtention = ".xlsx";
        public static readonly string xlsFileExtention = ".xls";
        public static readonly string SuccessText = "SUCCESS";
        public static readonly string FailedText = "FAILED";
        public static readonly string RequestStatus = "STATUS";
        public static readonly string FileRowNo = "FileRowNo";
        public static readonly string exceptionFileName = "ExceptionLog_";
        public static readonly string exceptionFileExtention = ".txt";
    }
}
