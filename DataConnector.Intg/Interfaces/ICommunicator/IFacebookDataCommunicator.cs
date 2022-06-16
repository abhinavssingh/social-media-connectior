using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IFacebookDataCommunicator
    {
        List<FBAdEntity> GetFBAdsMasterData(string lastRunDate = null, RequestModel requestModel = null);
    }
}
