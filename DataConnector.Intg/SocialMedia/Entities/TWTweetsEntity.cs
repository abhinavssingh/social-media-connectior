namespace DataConnector.Intg.SocialMedia.Entities
{
    public class TWTweetsEntity : TWLineItemEntity
    {        
        public string PromotedTweetID { get; set; }      
        public string TweetID { get; set; }
        public string WebsiteURL { get; set; }
        public string TweetStatus { get; set; }
    }
}
