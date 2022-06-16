using DataConnector.Intg.SocialMedia.Common;
namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IDv360ApiCommunicator
    {
        string GetCreativeURL(long advertiseID, long creativeID);
        string GetReportURLByQueryID(Enums.DV360QueryType queryType, long queryID);
        string[] GetCustomReport(Enums.DV360QueryType queryType, long startDateTimeMilliseconds = 0, long endDateTimeMilliseconds = 0);
        void DeleteQuery(long queryID);
    }
}
