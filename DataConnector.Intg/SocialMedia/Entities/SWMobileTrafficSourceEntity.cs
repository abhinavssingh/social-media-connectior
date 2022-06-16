using System.Collections.Generic;

namespace DataConnector.Intg.SocialMedia.Entities
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SWMobileTrafficSourceEntity
    {  
        public List<SWMobileTrafficSourceList> MobileOverview { get; set; }
    }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SWMobileTrafficSourceList
    {        
        public string source_type { get; set; }
        public List<SWMobileTrafficSourceVisits> visits { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SWMobileTrafficSourceVisits
    { 
        public string date { get; set; }
        public decimal visits { get; set; }
    }


}
