using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;
using System.Data;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IGoogleDataCommunicator
    {
        List<GAnayticsMasterEntity> GetDataList(string dataSet, string[] listViewIDs, RequestModel requestModel = null, string lastRunDate = null);
        DataTable Convertor(List<GAnayticsMasterEntity> result, string dataSet);
    }
}
