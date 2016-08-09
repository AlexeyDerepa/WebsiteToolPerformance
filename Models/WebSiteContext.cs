using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace WSP_2.Models
{
    public class WebSiteContext : DbContext
    {
        public DbSet<FoundWebSiteAddress> Addresses { get; set; }
        public DbSet<FoundSiteMape> FoundSiteMapes { get; set; }
        public DbSet<FoundSitePage> FoundSitePages { get; set; }

        public WebSiteContext() : base("Connection") { }
        //static WebSiteContext()
        //{
        //    Database.SetInitializer<WebSiteContext>(new ContextInitializer());
        //}
    }
}