namespace DataConnector.Intg.SocialMedia.Entities
{
    public class DV360MasterEntity
    {
        public int FileRowNo { get; set; }
        public string Date { get; set; }               
        public string LineItem { get; set; }
        public string LineItemID { get; set; }
        public string Campaign { get; set; }
        public string CampaignID { get; set; }
        public string Creative { get; set; }
        public string CreativeID { get; set; }
        public string FloodlightActivityName { get; set; }
        public string FloodlightActivityID { get; set; }        
        public string Impressions { get; set; }
        public string Clicks { get; set; }
        public string Revenue { get; set; }
        public string TotalConversions { get; set; }
        public string PostClickConversions { get; set; }
        public string PostViewConversions { get; set; }
        public string AdPosition { get; set; }
        public string Platform { get; set; }
        public string URL { get; set; }
    }
}
