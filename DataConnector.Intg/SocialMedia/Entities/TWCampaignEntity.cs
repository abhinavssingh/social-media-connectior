using System;

namespace DataConnector.Intg.SocialMedia.Entities
{
    public class TWCampaignEntity
    {
        public string CampaignID { get; set; }      
        public string CampaignName { get; set; }
        public DateTime StartDate { get; set; }       
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
    }
}
