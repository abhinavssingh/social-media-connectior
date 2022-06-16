using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IDv360DataCommunicator
    {
        List<DV360MasterEntity> GetDV360AdsMasterData(string lastRunDate = null, RequestModel requestModel = null);
    }
}
