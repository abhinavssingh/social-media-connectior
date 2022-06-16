using DataConnector.Intg.SocialMedia.Entities;
using System.Collections.Generic;

namespace DataConnector.Intg.Interfaces.ICommon
{
    public interface ISocialHelper
    {
        List<YearsMonths> GetBusinessDatesList(string lastRunDate = null, RequestModel requestModel = null, int dayChunkSize = 7, string socialMediaType = null);
    }
}
