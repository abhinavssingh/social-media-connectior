namespace DataConnector.Intg.SocialMedia.Entities
{
    public class FBAdEntity
    {   
        public int FileRowNo { get; set; }        
        public string AccountID { get; set; }    
        public string AccountName { get; set; }    
        public string CapaignID { get; set; }    
        public string CampaignName { get; set; }
        public string AdID { get; set; }      
        public string AdName { get; set; }
        public string AdSetID { get; set; }       
        public string AdSetName { get; set; }   
        public string Spend { get; set; }      
        public string Impressions { get; set; }
        public string Post_Engagement { get; set; }
        public string Clicks_All { get; set; }
        public string Link_Clicks { get; set; }
        public string Link_Clicks_Unique { get; set; }
        public string Cost_Per_ThruPlay { get; set; }
        public string Video_ThruPlay_Watched_Actions { get; set; }
        public string Date_Start { get; set; }
        public string Date_Stop { get; set; }
        public string Platform { get; set; }
        public string Platform_Placement { get; set; }
        public string WebsiteURL { get; set; }
    }
}
