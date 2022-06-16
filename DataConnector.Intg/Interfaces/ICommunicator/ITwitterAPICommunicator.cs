using DataConnector.Intg.SocialMedia.Communicator;
using System.Net;

namespace DataConnector.Intg.Interfaces.ICommunicator
{
    public interface ITwitterApiCommunicator
    {  
        WebResponse GetResponse(string resourceUrl, Method method, string cursor=null);        
    }
}
