using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WSP_2.Models
{
    public class Processing
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

        private volatile bool _shouldStop;
        public  List<long[]> listForFlotcharts;
        public volatile float numberForFloat;

        public Processing()
        {
            db = new WebSiteContext();
            threadLock = new object();
            listForFlotcharts = new List<long[]>();
            sw = new System.Diagnostics.Stopwatch();
        }
        public int? ProcessingDeep(string hostName)
        {
            hostName = MathcAddress(hostName, httpwebAddress, 0);

            //check in the existence of the site
            if (SiteIPAddress(hostName).Count == 0)
                return null;// new List<string>() { "site does't find" };
            listForFlotcharts = new List<long[]>();
            wsa = new FoundWebSiteAddress { UrlAddress = hostName, isExistAddress = true };

            //Save request by url
            db.Addresses.Add(wsa);
            db.SaveChanges();

            SearchSitePage(
                SearchSiteMap(hostName)
                );

            return wsa.Id;
        }
        private void SearchSitePage(List<string> listSiteMap)
        {
            List<TimeSpan> lts;
            foreach (string xmlPage in listSiteMap)
            {
                lts = SpeedMeasurement(xmlPage);

                info.listSiteMap = SearchPattern(info.str, httpwebPage, 1);

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
                    foreach (var thread in six)
                    {
                        thread.Join();
                    }
                    intSix = 0;
                    six = new List<System.Threading.Thread>();
                }
            }

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

                db.FoundSitePages.Add(new FoundSitePage { FoundSiteMape = fsm, NameSitePage = page, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)) });
                numberForFloat = (float)lts.First().Ticks;
                listForFlotcharts.Add(new long[] { listForFlotcharts.Count, (long)lts.First().Ticks });//for flot chart 
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
        public List<long[]> GetArrayForFlot()
        {
            float a = numberForFloat;
            //lock (threadLock)
            //{
            //    if (listForFlotcharts.Count < 502)
            //    {
                    return listForFlotcharts;
            //    }

            //    if (listForFlotcharts.Count > 3000)
            //    {
            //        listForFlotcharts = listForFlotcharts.GetRange(listForFlotcharts.Count - 502, 500);
            //    }
            //return listForFlotcharts.GetRange(listForFlotcharts.Count - 502, 500);
            //}

        }

    }
}