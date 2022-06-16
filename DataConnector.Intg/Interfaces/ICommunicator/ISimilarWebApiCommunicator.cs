using DataConnector.Intg.SocialMedia.Entities;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface ISimilarWebApiCommunicator
    {
        SWDesktopTrafficSourceEntity GetSWDesktopTrafficeData(string adsDataURI, int delayTime);
        SWMobileTrafficSourceEntity GetSWMobileTrafficeData(string adsDataURI, int delayTime);
    }
}
