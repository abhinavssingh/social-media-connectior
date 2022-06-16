namespace DataConnector.Intg.SocialMedia.Entities
{
    public class GAnalyticsPageEventEntity : GAnayticsMasterEntity
    {     
        public string EventCategory { get; set; }
        public string EventAction { get; set; }
        public string EventLabel { get; set; }
        public string Sessions { get; set; }
        public string UniqueEvents { get; set; }
        public string TotalEvents { get; set; }
        public string SessionsWithEvent { get; set; }
        public string PagePath { get; set; }

    }
}
