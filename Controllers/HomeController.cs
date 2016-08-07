using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WSP_2.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        Models.Processing2 proc = new Models.Processing2();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SearchDeepSiteMape(WSP_2.Models.FoundWebSiteAddress hostName)
        {
            return PartialView(proc.ProcessingDeep(hostName));
        }

        public ActionResult PingSite()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Ping(WSP_2.Models.FoundWebSiteAddress hostName)
        {
            return PartialView(proc.Ping(hostName.UrlAddress));
        }

//////////////////////// ASYNC REQUEST /////////////////////////////////////////////////////////////
        public async System.Threading.Tasks.Task<ActionResult> HistoryRequests()
        {
            return View(await proc.HistoryRequests());
        }
        public async System.Threading.Tasks.Task<ActionResult> HistoryXml(int id)
        {
            return View(await proc.HistoryXml(id));
        }
        public async System.Threading.Tasks.Task<ActionResult> SearchSitePage(int id)
        {
            return View(await proc.SearchSitePage(id));
        }
        public async System.Threading.Tasks.Task<ActionResult> SpecificPartPages(int id)
        {
            return View("SearchSitePage",await proc.SpecificPartPages(id));
        }
//////////////////////// JSON REQUEST /////////////////////////////////////////////////////////////
        public  JsonResult JsonForFlotcharts(string guid)
        {
            var result =  proc.GetArrayForFlot(guid);

            return Json(result.ToArray(), JsonRequestBehavior.AllowGet);
        }

    }
}
