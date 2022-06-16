namespace DataConnector.Intg.SocialMedia.Entities
{
    public class TWLineItemEntity : TWCampaignEntity
    {        
        public string LineItemID { get; set; }      
        public string LineItemName { get; set; }
        public string AdvertiserUserID { get; set; }       
        public string LineItemStatus { get; set; }
    }
}
