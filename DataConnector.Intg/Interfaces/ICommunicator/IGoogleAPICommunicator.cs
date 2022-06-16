using Google.Apis.AnalyticsReporting.v4.Data;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IGoogleApiCommunicator
    {
        GetReportsResponse ExecuteRequest(List<Dimension> listDimension, List<Metric> listMetric, DateRange dateRange,
                                                    string gaViewID, string pageToken);        
    }
}
