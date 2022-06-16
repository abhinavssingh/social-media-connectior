namespace DataConnector.Intg.SocialMedia.Common
{
    public class Enums
    {
        public enum EncryptStatus
        {
            Success,
            Failed,
            FilesNotFound            
        }
        
        public enum DV360QueryType
        {
            Today,
            PreviousYear,
            YearToDate,
            Custom
        }

        public enum GADataType
        {
            GAPAGEDATA,
            GAEVENTDATA,
            GAGEODATA,
            GAPAGEEVENTDATA,
            GACUSTOMDATA
        }

        public enum GATireDataType
        {
            GATIREPAGEDATA,
            GATIREPAGEEVENTDATA,
            GATIRECUSTOMDATA
        }        
    }
}
