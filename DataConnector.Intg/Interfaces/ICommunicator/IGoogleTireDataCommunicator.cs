using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;
using System.Data;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IGoogleTireDataCommunicator
    {
        List<GATireMasterEntity> GetTireDataList(string dataSet, string[] listViewIDs, RequestModel requestModel = null, string lastRunDate = null);
        DataTable Convertor(List<GATireMasterEntity> result, string dataSet);
    }
}
