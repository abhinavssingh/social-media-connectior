using DataConnector.Intg.SocialMedia.Entities;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface IFacebookApiCommunicator
    {
        RootObjectAds GetAdsDataListbyBreakDown(string adsDataURI, bool fullDataCheck, int delayTime);
        string GetdataByURI(string URI, bool flgDelay, int delayTime = 60000);
    }
}
