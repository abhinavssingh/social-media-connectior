namespace DataConnector.Intg.SocialMedia.Entities
{
    public class GATireEventEntity : GATireMasterEntity
    {
        public string DefaultChannelGrouping { get; set; }
        public string PagePath { get; set; }
        public string EventCategory { get; set; }
        public string EventAction { get; set; }
        public string EventLabel { get; set; }
        public string UniqueEvents { get; set; }
    }
}
