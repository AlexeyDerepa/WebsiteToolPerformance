using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WSP_2.Models
{
    public class FoundSiteMape
    {
        public int Id { get; set; }
        public string NameSateMape { get; set; }
        public TimeSpan? TimeMin { get; set; }
        public TimeSpan? TimeMax { get; set; }
        public TimeSpan? TimeAverage { get; set; }
        public string Contents { get; set; }


        public int? FoundWebSiteAddressId { get; set; }
        public virtual FoundWebSiteAddress FoundWebSiteAddress { get; set; }

        public virtual ICollection<FoundSitePage> FoundSitePages { get; set; }
        public FoundSiteMape()
        {
            FoundSitePages = new List<FoundSitePage>();
        }

    }
}