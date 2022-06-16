using System;

namespace DataConnector.Intg.SocialMedia.Common
{
    public class ApplicationSettings
    {
        public string FBGraphAPIUrl { get; set; }
        public string FBAccountID { get; set; }        
        public string FBLeadAdsAccessToken { get; set; }
        public string AdsURI { get; set; }
        public string FBAdsDataURI { get; set; }
        public string FBDefaultDate { get; set; }
        public string FBFullLoadForMonth { get; set; }
        public string FBAPICallDelay { get; set; }
        public string FBFullDataCheck { get; set; }
        public string FBDeltaDataCheck { get; set; }
        public string AzureWebJobsStorage { get; set; }
        public string FBFilePath { get; set; }
        public string FBLeadsFilePath { get; set; }
        public string FBFileName { get; set; }
        public string FBLeadsFileName { get; set; }
        public string FBStatusFilePath { get; set; }
        public string FBStatusFileName { get; set; }
        public string FBLastRunPath { get; set; }
        public string FBLastRunFileName { get; set; }
        public string FBSessionFilePath { get; set; }
        public string FBSessionFileName { get; set; }
        public string FBPlatformDayChunkValue { get; set; }
        public string FBAdsCreativeIDDataURI { get; set; }
        public string FBAdsPageIDDataURI { get; set; }
        public string FBAdsLinkURLURI { get; set; }
        public string FBExceptionPath { get; set; }


        public string GADefaultDate { get; set; }
        public string GAFullDataCheck { get; set; }
        public string GADeltaDataCheck { get; set; }
        public string GAFilePath { get; set; }
        public string GAStatusFilePath { get; set; }
        public string GAStatusFileName { get; set; }
        public string GALastRunPath { get; set; }
        public string GALastRunFileName { get; set; }
        public string GASessionFilePath { get; set; }
        public string GASessionFileName { get; set; }
        public string GAExceptionPath { get; set; }
        public string GADelayTime { get; set; }


        public string TWAdsAPIUrl { get; set; }
        public string TWAccountID { get; set; }
        public string TWAccountName { get; set; }        
        public string TWAdsSynchronousDataURI { get; set; }
        public string TWAdsAsynchronousDataURI { get; set; }
        public string TWAdsCampaignDataURI { get; set; }
        public string TWAdsLineItemDataURI { get; set; }
        public string TWAdsPromotedTweetsDataURI { get; set; }
        public string TWAdsTweetsDataURI { get; set; }
        public string TWDefaultDate { get; set; }
        public string TWFullLoadForMonth { get; set; }        
        public string TWFullDataCheck { get; set; }
        public string TWDeltaDataCheck { get; set; }
        public string TWFilePath { get; set; }
        public string TWStatusFilePath { get; set; }
        public string TWStatusFileName { get; set; }
        public string TWLastRunPath { get; set; }
        public string TWLastRunFileName { get; set; }
        public string TWSessionFilePath { get; set; }
        public string TWSessionFileName { get; set; }
        public string TWExceptionPath { get; set; }
        public string TWFileNameForAdsData { get; set; }
        public string TWPlacementTwitter { get; set; }
        public string TWPlacementTwitterAudience { get; set; }
        public string TWDayChunkValue { get; set; }


        public string DVDeltaQueryID { get; set; }
        public string DVCreateTodayQuery { get; set; }
        public string DVFullQueryID { get; set; }
        public string DVCreateFullQuery { get; set; }
        public string DVFilePath { get; set; }
        public string DVStatusFileName { get; set; }
        public string DVLastRunFileName { get; set; }
        public string DVSessionFileName { get; set; }
        public string DV360AdsDataFileName { get; set; }
        public string DVDefaultDate { get; set; }
        public string DVDataSeprationDate { get; set; }
        public string DVAdvertiseFilterType { get; set; }
        public string DVAdvertiseFilterTypeValue_FullLoad { get; set; }
        public string DVAdvertiseFilterTypeValue_DeltaLoad { get; set; }
        public string DVCampaignFilterType { get; set; }
        public string DVCampaignFilterTypeValue_PreYear { get; set; }
        public string DVCampaignFilterTypeValue_CurYear { get; set; }
        public string DVCampaignFilterTypeValue_Today { get; set; }
        public string DVReportDimensions { get; set; }
        public string DVReportMetrics { get; set; }
        public string DVReportType { get; set; }
        public string SWAPIUrl { get; set; }         
        public string SWDesktopTrafficSourceUrl { get; set; }
        public string SWDesktopPartialTrafficSourceUrl { get; set; }
        public string SWMobilePartialTrafficeSourceUrl { get; set; }
        public string SWMobileTrafficeSourceUrl { get; set; }
        public string SWFilePath { get; set; }
        public string SWStatusFileName { get; set; }
        public string SWLastRunFileName { get; set; }
        public string SWSessionFileName { get; set; }
        public string SWDesktopDataFileName { get; set; }
        public string SWMobileDataFileName { get; set; }
        public string SWDataWebsites { get; set; }        
        public string SWDelayTime { get; set; }
        public string SWMobileDataRequired { get; set; }
        public string GATireStatusFileName { get; set; }
        public string GATireLastRunFileName { get; set; }
        public string GATireSessionFileName { get; set; }

        public ApplicationSettings()
        {    
            FBAccountID = Environment.GetEnvironmentVariable("FBAccountID");
            FBGraphAPIUrl = Environment.GetEnvironmentVariable("FBGraphAPIUrl");            
            FBAdsDataURI = Environment.GetEnvironmentVariable("FBAdsDataURI");            
            FBLeadAdsAccessToken = Environment.GetEnvironmentVariable("FBLeadAdsAccessToken");
            FBDefaultDate = Environment.GetEnvironmentVariable("FBDefaultDate");
            FBFullLoadForMonth = Environment.GetEnvironmentVariable("FBFullLoadForMonth");
            FBAPICallDelay = Environment.GetEnvironmentVariable("FBAPICallDelay");
            AzureWebJobsStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            FBFilePath = Environment.GetEnvironmentVariable("FBFilePath");
            FBFileName = Environment.GetEnvironmentVariable("FBFileName").Replace("Date", DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            FBLeadsFilePath = Environment.GetEnvironmentVariable("FBLeadsFilePath");
            FBLeadsFileName = Environment.GetEnvironmentVariable("FBLeadsFileName");
            FBStatusFilePath = Environment.GetEnvironmentVariable("FBFilePath");
            FBStatusFileName = Environment.GetEnvironmentVariable("FBStatusFileName");
            FBLastRunPath = Environment.GetEnvironmentVariable("FBFilePath");
            FBLastRunFileName = Environment.GetEnvironmentVariable("FBLastRunFileName");
            FBSessionFilePath = Environment.GetEnvironmentVariable("FBFilePath");
            FBSessionFileName = Environment.GetEnvironmentVariable("FBSessionFileName");
            FBPlatformDayChunkValue = Environment.GetEnvironmentVariable("FBPlatformDayChunkValue");
            FBAdsCreativeIDDataURI = Environment.GetEnvironmentVariable("FBAdsCreativeIDDataURI");
            FBAdsPageIDDataURI = Environment.GetEnvironmentVariable("FBAdsPageIDDataURI");
            FBAdsLinkURLURI = Environment.GetEnvironmentVariable("FBAdsLinkURLURI");
            FBExceptionPath = Environment.GetEnvironmentVariable("FBExceptionPath");

            GADefaultDate = Environment.GetEnvironmentVariable("GADefaultDate");           
            GAFilePath = Environment.GetEnvironmentVariable("GAFilePath");
            GAStatusFilePath = Environment.GetEnvironmentVariable("GAFilePath");
            GAStatusFileName = Environment.GetEnvironmentVariable("GAStatusFileName");
            GALastRunPath = Environment.GetEnvironmentVariable("GAFilePath");
            GALastRunFileName = Environment.GetEnvironmentVariable("GALastRunFileName");
            GASessionFilePath = Environment.GetEnvironmentVariable("GAFilePath");
            GASessionFileName = Environment.GetEnvironmentVariable("GASessionFileName");
            GAExceptionPath = Environment.GetEnvironmentVariable("GAExceptionPath");
            GADelayTime = Environment.GetEnvironmentVariable("GADelayTime");

            TWAccountID = Environment.GetEnvironmentVariable("TWAccountID");
            TWAccountName = Environment.GetEnvironmentVariable("TWAccountName");
            TWAdsAPIUrl = Environment.GetEnvironmentVariable("TWAdsAPIUrl");
            TWAdsAsynchronousDataURI = Environment.GetEnvironmentVariable("TWAdsAsynchronousDataURI");
            TWAdsSynchronousDataURI = Environment.GetEnvironmentVariable("TWAdsSynchronousDataURI");
            TWAdsCampaignDataURI = Environment.GetEnvironmentVariable("TWAdsCampaignDataURI");
            TWAdsLineItemDataURI = Environment.GetEnvironmentVariable("TWAdsLineItemDataURI");
            TWAdsPromotedTweetsDataURI = Environment.GetEnvironmentVariable("TWAdsPromotedTweetsDataURI");
            TWAdsTweetsDataURI = Environment.GetEnvironmentVariable("TWAdsTweetsDataURI");            
            TWDefaultDate = Environment.GetEnvironmentVariable("TWDefaultDate");
            TWFullLoadForMonth = Environment.GetEnvironmentVariable("TWFullLoadForMonth");            
            TWFilePath = Environment.GetEnvironmentVariable("TWFilePath");
            TWStatusFilePath = Environment.GetEnvironmentVariable("TWFilePath");
            TWStatusFileName = Environment.GetEnvironmentVariable("TWStatusFileName");
            TWLastRunPath = Environment.GetEnvironmentVariable("TWFilePath");
            TWLastRunFileName = Environment.GetEnvironmentVariable("TWLastRunFileName");
            TWSessionFilePath = Environment.GetEnvironmentVariable("TWFilePath");
            TWSessionFileName = Environment.GetEnvironmentVariable("TWSessionFileName");            
            TWExceptionPath = Environment.GetEnvironmentVariable("TWExceptionPath");
            TWFileNameForAdsData = Environment.GetEnvironmentVariable("TWFileNameForAdsData");
            TWPlacementTwitter = Environment.GetEnvironmentVariable("TWPlacementTwitter");
            TWPlacementTwitterAudience = Environment.GetEnvironmentVariable("TWPlacementTwitterAudience");
            TWDayChunkValue = Environment.GetEnvironmentVariable("TWDayChunkValue");

            DVDeltaQueryID = Environment.GetEnvironmentVariable("DVDeltaQueryID");
            DVCreateTodayQuery = Environment.GetEnvironmentVariable("DVCreateTodayQuery");
            DVFullQueryID = Environment.GetEnvironmentVariable("DVFullQueryID");
            DVCreateFullQuery = Environment.GetEnvironmentVariable("DVCreateFullQuery");
            DVFilePath = Environment.GetEnvironmentVariable("DVFilePath");
            DVStatusFileName = Environment.GetEnvironmentVariable("DVStatusFileName");
            DVLastRunFileName = Environment.GetEnvironmentVariable("DVLastRunFileName");
            DVSessionFileName = Environment.GetEnvironmentVariable("DVSessionFileName");
            DV360AdsDataFileName = Environment.GetEnvironmentVariable("DV360AdsDataFileName").Replace("Date", DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            DVDefaultDate = Environment.GetEnvironmentVariable("DVDefaultDate");
            DVDataSeprationDate = Environment.GetEnvironmentVariable("DVDataSeprationDate");
            DVAdvertiseFilterType = Environment.GetEnvironmentVariable("DVAdvertiseFilterType");
            DVAdvertiseFilterTypeValue_FullLoad = Environment.GetEnvironmentVariable("DVAdvertiseFilterTypeValue_FullLoad");
            DVAdvertiseFilterTypeValue_DeltaLoad = Environment.GetEnvironmentVariable("DVAdvertiseFilterTypeValue_DeltaLoad");
            DVCampaignFilterType = Environment.GetEnvironmentVariable("DVCampaignFilterType");
            DVCampaignFilterTypeValue_PreYear = Environment.GetEnvironmentVariable("DVCampaignFilterTypeValue_PreYear");
            DVCampaignFilterTypeValue_CurYear = Environment.GetEnvironmentVariable("DVCampaignFilterTypeValue_CurYear");
            DVCampaignFilterTypeValue_Today = Environment.GetEnvironmentVariable("DVCampaignFilterTypeValue_Today");
            DVReportDimensions = Environment.GetEnvironmentVariable("DVReportDimensions");
            DVReportMetrics = Environment.GetEnvironmentVariable("DVReportMetrics");
            DVReportType = Environment.GetEnvironmentVariable("DVReportType");

            SWAPIUrl = Environment.GetEnvironmentVariable("SWAPIUrl");                       
            SWDesktopTrafficSourceUrl = Environment.GetEnvironmentVariable("SWDesktopTrafficSourceUrl");
            SWDesktopPartialTrafficSourceUrl = Environment.GetEnvironmentVariable("SWDesktopPartialTrafficSourceUrl");
            SWMobilePartialTrafficeSourceUrl = Environment.GetEnvironmentVariable("SWMobilePartialTrafficeSourceUrl");
            SWMobileTrafficeSourceUrl = Environment.GetEnvironmentVariable("SWMobileTrafficeSourceUrl");
            SWFilePath = Environment.GetEnvironmentVariable("SWFilePath");
            SWStatusFileName = Environment.GetEnvironmentVariable("SWStatusFileName");
            SWLastRunFileName = Environment.GetEnvironmentVariable("SWLastRunFileName");
            SWSessionFileName = Environment.GetEnvironmentVariable("SWSessionFileName");
            SWDesktopDataFileName = Environment.GetEnvironmentVariable("SWDesktopDataFileName").Replace("Date", DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            SWMobileDataFileName = Environment.GetEnvironmentVariable("SWMobileDataFileName").Replace("Date", DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            SWDataWebsites = Environment.GetEnvironmentVariable("SWDataWebsites");            
            SWDelayTime = Environment.GetEnvironmentVariable("SWDelayTime");
            SWMobileDataRequired = Environment.GetEnvironmentVariable("SWMobileDataRequired");
                     
            GATireStatusFileName = Environment.GetEnvironmentVariable("GATireStatusFileName");            
            GATireLastRunFileName = Environment.GetEnvironmentVariable("GATireLastRunFileName");            
            GATireSessionFileName = Environment.GetEnvironmentVariable("GATireSessionFileName");
        }
    }
}
