namespace DataConnector.Intg.SocialMedia.Entities
{
    public class GAnalyticsPageEntity : GAnayticsMasterEntity
    {      
        public string Sessions { get; set; }
        public string PageViews { get; set; }
        public string UniquePageViews { get; set; }
        public string Entrances { get; set; }
        public string Bounces { get; set; }
        public string PagePath { get; set; }
        public string PageTitle { get; set; }
        public string AdContent { get; set; }
        public string Campaign { get; set; }


    }

    


}
