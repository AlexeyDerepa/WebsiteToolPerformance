using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;



namespace WSP_2.Models
{
    public class Processing2
    {
        private WebSiteContext db;
        private FoundWebSiteAddress wsa;
        private FoundSiteMape fsm;

        private System.Diagnostics.Stopwatch sw;

        private Info info;

        private string httpwebAddress = @"https?://(www\.)?([\w\d\-]+(\.[\w\d]+)+)";
        private string webAddress = @"(www\.)?([\w\d\-]+(\.[\w\d]+)+)";
        private string httpwebXml = @"https?[\w\d/\:\.\-_]+\.xml";
        private string httpwebPage = @"<loc>([\w\W]+?)</loc>";

        private object threadLock;

        public  List<long[]> listForFlotcharts;
        public volatile float numberForFloat;
        private long counter = 0;
        public Processing2()
        {
            db = new WebSiteContext();
            threadLock = new object();
            listForFlotcharts = new List<long[]>();
            sw = new System.Diagnostics.Stopwatch();
        }
        public List<string> ProcessingDeep(FoundWebSiteAddress hostName)
        {
            hostName.UrlAddress = MathcAddress(hostName.UrlAddress, httpwebAddress, 0);

            //check in the existence of the site
            if (SiteIPAddress(hostName.UrlAddress).Count == 0)
                return new List<string>{"site does not exist"};
            listForFlotcharts = new List<long[]>();
            wsa = new FoundWebSiteAddress { UrlAddress = hostName.UrlAddress, isExistAddress = true, GuidString=hostName.GuidString };

            //Save request by url
            db.Addresses.Add(wsa);
            db.SaveChanges();

            SearchSitePage(
                SearchSiteMap(hostName.UrlAddress)
                );
            var xml = db.FoundSiteMapes.Where(x => x.FoundWebSiteAddressId == wsa.Id).Select(x => x.NameSateMape).ToList();

            List<string> list = new List<string> { "you can go to the history of requests", "URL address: ", wsa.UrlAddress, "Found *.xml: " + xml.Count().ToString() };
            list.AddRange(xml);
            list.Add(counter+" pages were found");
            return list;
        }
        private void SearchSitePage(List<string> listSiteMap)
        {
            List<TimeSpan> lts;
            foreach (string xmlPage in listSiteMap)
            {
                lts = SpeedMeasurement(xmlPage);

                info.listSiteMap = SearchPattern(info.str, httpwebPage, 1);

                if (info.listSiteMap.Count == 0)
                    continue;

                fsm = new FoundSiteMape { FoundWebSiteAddress = wsa, NameSateMape = xmlPage, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)) };

                db.FoundSiteMapes.Add(fsm);
                db.SaveChanges();

                SaveListPageThreadPool(info.listSiteMap, fsm);
            }
        }
        private List<string> SearchSiteMap(string hostName)
        {
            List<string> listSiteMap = new List<string>();
            info = RequestByUrl(hostName + "/robots.txt");
            listSiteMap = SearchPattern(info.str, httpwebXml);        //search in robots.txt files with *.xml
            info = RequestByUrl(hostName + "/sitemap.xml");
            info.listSiteMap = SearchPattern(info.str, httpwebXml);   //search in sitemap.xml files with *.xml 

            foreach (string item in info.listSiteMap)
                if (!listSiteMap.Contains(item))
                    listSiteMap.Add(item);

            if (!listSiteMap.Contains(hostName + "/sitemap.xml"))
                listSiteMap.Add(hostName + "/sitemap.xml");

            listSiteMap = SearchDeepSiteMap(listSiteMap);

            return listSiteMap;
        }
        private List<string> SearchDeepSiteMap(List<string> listSiteMap)
        {
            List<string> additionXml = listSiteMap.ToList();
            List<string> infos = new List<string>();
            bool flag = false;
            foreach (string xml in listSiteMap)
            {
                infos = SearchPattern(RequestByUrl(xml).str, httpwebXml);
                if (infos.Count > 0)
                {
                    additionXml.Remove(xml);
                    foreach (string item in infos)
                    {
                        if (!additionXml.Contains(item))
                        {
                            additionXml.Add(item);
                            flag = true;
                        }
                    }
                }
            }
            if (flag)
            {
                additionXml = SearchDeepSiteMap(additionXml);
            }
            return additionXml;
        }
        private List<TimeSpan> SpeedMeasurement(string address)
        {
            List<TimeSpan> lts = new List<TimeSpan>();
            for (int i = 0; i < 5; i++)
            {
                sw.Reset();
                sw.Start();
                info = RequestByUrl(address);
                sw.Stop();
                lts.Add(sw.Elapsed);
            }
            lts.Sort();
            return lts;
        }
        private List<string> SearchPattern(string target, string pattern, int num = 0)
        {
            List<string> list = new List<string>();

            if (string.IsNullOrEmpty(target))
                return list;

            System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.MatchCollection matches = regEx.Matches(target);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                list.Add(m.Groups[num].Value);
            }
            return list;
        }

        private  void SaveListPageThreadPool(List<string> list, FoundSiteMape fsm)
        {
            if (list.Count == 0)
                return;

            List<System.Threading.Thread> listTreads = new List<System.Threading.Thread>();
            List<System.Threading.Thread> six = new List<System.Threading.Thread>();
            int intSix = 0;
            int count = 0;

            foreach (string page in list)
            {
                listTreads.Add(new System.Threading.Thread(() => Proc(page, fsm)));
            }
            foreach (var item in listTreads)
            {
                count++;
                item.Start();
                intSix++;
                six.Add(item);
                if (intSix == 6)
                {
                    SaveChangesInDb(six);
                    intSix = 0;
                    six = new List<System.Threading.Thread>();
                }
            }
            SaveChangesInDb(six);
        }

        private void SaveChangesInDb(List<System.Threading.Thread> six)
        {
            foreach (var thread in six)
            {
                thread.Join();
            }
            db.SaveChanges();
        }
        private void Proc(string page, FoundSiteMape fsm)
        {
            List<TimeSpan> lts;
            TimeSpan ts;

            lts = new List<TimeSpan>();
            ts = new TimeSpan();
            for (int i = 0; i < 5; i++)
            {
                sw.Reset();
                sw.Start();
                RequestByUrl(page);
                sw.Stop();
                lts.Add(sw.Elapsed);
                ts += sw.Elapsed;
            }
            lts.Sort();
            lock (threadLock)
            {
                db.FoundSitePages.Add(new FoundSitePage { FoundSiteMape = fsm, NameSitePage = page, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)), Number = counter++ });
            }
        }
        private Info RequestByUrl(string hostName)
        {
            try
            {
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(new Uri(hostName));

                request.KeepAlive = false;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
                return new Info
                {
                    str = new System.IO.StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd(),
                    isFind = true
                };
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    return new Info
                    {
                        str = new System.IO.StreamReader(wex.Response.GetResponseStream()).ReadToEnd(),
                        isFind = true
                    };
                }
                else
                {
                    return new Info { error = wex.Message, isFind = false };
                }
            }
            catch (System.UriFormatException ufe)
            {
                return new Info { error = ufe.Message, isFind = false };
            }
        }

        private List<string> SiteIPAddress(string hostName)
        {
            string match = MathcAddress(hostName, webAddress, 2);

            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(match))
            {
                return list;
            }
            System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry(match);

            foreach (System.Net.IPAddress address in entry.AddressList)
            {
                list.Add(address.ToString());
            }
            return list;
        }

        private string MathcAddress(string hostName, string pattern, int number)
        {
            System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Match match = regEx.Match(hostName);

            return match.Groups[number].Value;
        }
        public List<long[]> GetArrayForFlot(string guid)
        {
            var allInfo = (from page in db.FoundSitePages
                           join xml in db.FoundSiteMapes on page.FoundSiteMapeId equals xml.Id
                           join addr in db.Addresses on xml.FoundWebSiteAddressId equals addr.Id
                           where addr.GuidString == guid
                           select page).ToList();
            

           var allInfo2 = allInfo.Select(x => new long[] { (long)x.Number, x.TimeMin.Value.Ticks }).ToList();
           if (allInfo2.Count<305)
           {
           return allInfo2;

           }
           return allInfo2.GetRange(allInfo2.Count-302, 300);
        }
    #region History
		public async System.Threading.Tasks.Task<List<FoundWebSiteAddress>> HistoryRequests()
        {
            List<FoundWebSiteAddress> allInfo;
            try
            {
                allInfo = await db.Addresses.OrderByDescending(x => x.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                allInfo = new List<FoundWebSiteAddress>();
            }
            return allInfo;
        }
        public async System.Threading.Tasks.Task<List<FoundSiteMape>> HistoryXml(int id)
        {
            List<FoundSiteMape> allInfo;
            try
            {
                allInfo = await (from xml in db.FoundSiteMapes
                                 join addr in db.Addresses on xml.FoundWebSiteAddressId equals addr.Id
                                 where xml.FoundWebSiteAddressId == id
                                 select xml).OrderByDescending(x => x.TimeMin).ToListAsync();
            }
            catch (Exception ex)
            {
                allInfo = new List<FoundSiteMape>();
            }
            return allInfo;
        }
        public async System.Threading.Tasks.Task<List<FoundSitePage>> SearchSitePage(int id)
        {
            List<FoundSitePage> allInfo;
            try
            {
                allInfo = await (from page in db.FoundSitePages
                                 join xml in db.FoundSiteMapes on page.FoundSiteMapeId equals xml.Id
                                 join addr in db.Addresses on xml.FoundWebSiteAddressId equals addr.Id
                                 where addr.Id == id
                                 orderby page.TimeMin
                                 select page).OrderByDescending(x => x.TimeMin).ToListAsync();
            }
            catch (Exception ex)
            {
                allInfo = new List<FoundSitePage>();
            }
            return allInfo;
        }
        public async System.Threading.Tasks.Task<List<FoundSitePage>> SpecificPartPages(int id)
        {
            List<FoundSitePage> allInfo;
            try
            {
                allInfo = await (from page in db.FoundSitePages
                                 join xml in db.FoundSiteMapes on page.FoundSiteMapeId equals xml.Id
                                 where page.FoundSiteMapeId == id
                                 orderby page.TimeMin
                                 select page).OrderByDescending(x => x.TimeMin).ToListAsync();
            }
            catch (Exception ex)
            {
                allInfo = new List<FoundSitePage>();
            }
            return allInfo;
        }


	#endregion    
        public List<string> Ping(string hostName)
        {
            hostName = MathcAddress(hostName, httpwebAddress, 0);

            System.Net.IPHostEntry entry;
            List<string> list = new List<string>();
            try
            {
                entry = PingSite(hostName);
            }
            catch (Exception ex)
            {
                list.Add(ex.Message);
                return list;
            }
            list.Add("IP addresses:");
            string temp = "";
            foreach (System.Net.IPAddress address in entry.AddressList)
            {
                list.Add(address.ToString());
                temp = PingInfo(address.ToString());
                if (!string.IsNullOrEmpty(temp))
                    list.Add(temp);

            }

            if (entry.Aliases.Length > 0)
            {
                list.Add("Aliases:");
                foreach (string name in entry.Aliases)
                {
                    list.Add(name);
                }
            }
            list.Add("HostName:");
            list.Add(entry.HostName);

            return list;
        }
        private string PingInfo(string p)
        {

            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();

            options.DontFragment = true;

            string data = new string('a', 32);
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            System.Net.NetworkInformation.PingReply reply = pingSender.Send(p, timeout, buffer, options);

            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                return string.Format("\t Round Trip time: {1} ms, Time to live: {2}, Buffer size: {4}", reply.Address.ToString(), reply.RoundtripTime, reply.Options.Ttl, reply.Options.DontFragment, reply.Buffer.Length);
            }
            return "";
        }
        private System.Net.IPHostEntry PingSite(string hostName)
        {
            string match = MathcAddress(hostName, webAddress, 2);

            if (string.IsNullOrEmpty(match))
            {
                throw new System.Exception("Do net corectly web address");
            }

            try
            {
                return System.Net.Dns.GetHostEntry(match);
            }
            catch (Exception ex)
            {
                throw ex;// new System.Exception("Do net corectly web address");
            }
        }

    }
}