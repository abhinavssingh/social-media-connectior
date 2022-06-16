using System.Collections.Generic;

namespace DataConnector.Intg.SocialMedia.Entities
{
    public class SWDesktopTrafficSourceEntity
    {  
        public List<SWDesktopTrafficSourceList> DesktopOverview { get; set; }
    }
    public class SWDesktopTrafficSourceList
    {        
        public string source_type { get; set; }
        public List<SWDesktopTrafficSourceVisits> visits { get; set; }
    }

    public class SWDesktopTrafficSourceVisits
    { 
        public string date { get; set; }
        public decimal organic { get; set; }
        public decimal paid { get; set; }
    }


}
