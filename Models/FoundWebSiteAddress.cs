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

        [Required(ErrorMessage = "Нужно ввести адрес сайта")]
        [StringLength(150, MinimumLength = 6, ErrorMessage = "Длина строки должна быть от 6 до 150 символов")]
        [RegularExpression(@"https?[\w\W]+", ErrorMessage = "Некорректный адрес")]
        [Display(Name = "Адрес сайта")]
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
