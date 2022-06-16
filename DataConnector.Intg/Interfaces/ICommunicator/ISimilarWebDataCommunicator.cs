using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface ISimilarWebDataCommunicator
    {
        Dictionary<string, List<SWEntity>> GetSWMasterData(string startDate, string endDate);
    }
}
