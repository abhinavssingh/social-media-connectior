using System;

namespace DataConnector.Intg.SocialMedia.Entities
{
    public class TWAdsEntity : TWTweetsEntity
    {        
        public string Impressions { get; set; }
        public string Spend { get; set; }
        public string Post_Engagement { get; set; }
        public string Clicks_All { get; set; }
        public string Link_Clicks { get; set; }
        public string Link_Clicks_Unique { get; set; }
        public string Site_Visits { get; set; }
        public string Video_Completions { get; set; }        
        public string Platform_Placement { get; set; }
        public DateTime Created_Date { get; set; }

    }
}
