namespace DataConnector.Intg.SocialMedia.Entities
{
    public class GAnalyticsGeoEntity : GAnayticsMasterEntity
    {        
        public string City { get; set; }
        public string Region { get; set; }
        public string Sessions { get; set; }
        public string UniquePageViews { get; set; }

    }
}
