using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WSP_2.Models
{
    public class FoundWebSiteAddress
    {
        [ScaffoldColumn(false)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Enter the url of the website")]
        [StringLength(150, MinimumLength = 6, ErrorMessage = "String length must be from 6 to 150 characters")]
        [RegularExpression(@"https?[\w\W]+", ErrorMessage = "Incorrect address")]
        [Display(Name = "Website address")]
        public string UrlAddress { get; set; }

        [ScaffoldColumn(false)]
        public bool? isExistAddress { get; set; }

        //[ScaffoldColumn(false)]
    
        public string GuidString { get; set; }


        public virtual ICollection<FoundSiteMape> FoundSiteMapes { get; set; }
        public FoundWebSiteAddress()
        {
            FoundSiteMapes = new List<FoundSiteMape>();
        }
    }
}
