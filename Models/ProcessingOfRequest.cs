using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WSP_2.Models
{
    public class ProcessingOfRequest
    {
        private static System.Net.WebResponse _responce;
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private static Info info;
        private static FoundWebSiteAddress wsa;
        private static FoundSiteMape fsm;
        private static WebSiteContext db = new WebSiteContext();
        private static System.Net.WebRequest _request;
        private static System.Net.HttpWebRequest myHttpWebRequest;
        private static System.Net.HttpWebResponse myHttpWebResponse;
        private static string httpwebAddress =  @"https?://(www\.)?([\w\d\-]+(\.[\w\d]+)+)";
        private static string webAddress =      @"(www\.)?([\w\d\-]+(\.[\w\d]+)+)";
        private static string httpwebXml  =     @"https?[\w\d/\:\.\-_]+\.xml";
        private static string httpwebPage =     @"<loc>([\w\W]+?)</loc>";
        private static object threadLock = new object();
        private static List<long[]> listForFlotcharts = new List<long[]>();

        public static int? ProcessingDeep(string hostName)
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
        public static List<long[]> GetArrayForFlot()
        {
            if (listForFlotcharts.Count<501)
            {
                return listForFlotcharts;
            }
            return listForFlotcharts.GetRange(listForFlotcharts.Count - 500, 500);
        }
        public static int? ProcessingSurface(string hostName)
        {
            hostName = MathcAddress(hostName, httpwebAddress, 0);

            //check in the existence of the site
            if (SiteIPAddress(hostName).Count == 0)
                return null;// new List<string>() { "site does't find" };

            wsa = new FoundWebSiteAddress { UrlAddress = hostName, isExistAddress = true };

            //Save request by url
            db.Addresses.Add(wsa);
            db.SaveChanges();

            SaveListXML(SearchSiteMap(hostName), wsa);
            return wsa.Id;

        }

        /// <summary>
        /// search site map in robots.txt and sitemap.xml
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        private static List<string> SearchSiteMap(string hostName)
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

        private static List<string> SearchDeepSiteMap(List<string> listSiteMap)
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

        private static void SearchSitePage(List<string> listSiteMap)
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
        private static List<TimeSpan> SpeedMeasurement(string address)
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
        private static void SaveListPageThreadPool(List<string> list, FoundSiteMape fsm)
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

        private static void Proc(string page, FoundSiteMape fsm)
        {
            List<TimeSpan> lts;
            TimeSpan ts;

            lts = new List<TimeSpan>();
            ts = new TimeSpan();
            for (int i = 0; i < 5; i++)
            {
                sw.Reset();
                sw.Start();
                RequestByUrlForThread(page);
                sw.Stop();
                lts.Add(sw.Elapsed);
                ts += sw.Elapsed;
            }
            lts.Sort();
            lock (threadLock)
            {
                db.FoundSitePages.Add(new FoundSitePage { FoundSiteMape = fsm, NameSitePage = page, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)) });
                listForFlotcharts.Add(new long[] { listForFlotcharts.Count, (long)lts.First().Ticks });//for flot chart 
            }
        }

        private static void SaveListXML(List<string> list, FoundWebSiteAddress fwsa)
        {
            List<TimeSpan> lts;
            foreach (string xml in list)
            {
                lts = SpeedMeasurement(xml);
                db.FoundSiteMapes.Add(new FoundSiteMape { FoundWebSiteAddress = fwsa, NameSateMape = xml, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)) });
            }
            db.SaveChanges();
        }

        private static void SaveListPage(List<string> list, FoundSiteMape fsm)
        {
            List<TimeSpan> lts;
            foreach (string page in list)
            {
                lts = SpeedMeasurement(page);
                db.FoundSitePages.Add(new FoundSitePage { FoundSiteMape = fsm, NameSitePage = page, TimeMax = lts.Last(), TimeMin = lts.First(), TimeAverage = new TimeSpan((long)lts.Average(x => x.Ticks)) });
                // break;
            }
            db.SaveChanges();
        }


        private static Info RequestByUrl(string hostName)
        {
            //return RequestByUrl2(hostName);
            try
            {
                _responce = System.Net.WebRequest.Create(new Uri(hostName)).GetResponse();//создаем объект ответа
            }
            catch (System.Net.WebException wex)
            {
                return new Info { error = wex.Message, isFind = false };
            }
            catch (System.UriFormatException ufe)
            {
                return new Info { error = ufe.Message, isFind = false };
            }
            return new Info
            {
                str = new System.IO.StreamReader(_responce.GetResponseStream()).ReadToEnd(),
                isFind = true
            };
        }
        private static Info RequestByUrlForThread(string hostName)
        {
            System.Net.WebResponse responce;
            try
            {
                responce = System.Net.WebRequest.Create(new Uri(hostName)).GetResponse();//создаем объект ответа
            }
            catch (System.Net.WebException wex)
            {
                return new Info { error = wex.Message, isFind = false };
            }
            catch (System.UriFormatException ufe)
            {
                return new Info { error = ufe.Message, isFind = false };
            }
            return new Info
            {
                str = new System.IO.StreamReader(responce.GetResponseStream()).ReadToEnd(),
                isFind = true
            };
        }
        private static Info RequestByUrl2(string hostName)
        {
            myHttpWebRequest = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(hostName);
            myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0";
            myHttpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";// "image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            myHttpWebRequest.Method = "GET";// System.Net.Http.HttpMethod.Get; // метод GET

            myHttpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
            myHttpWebRequest.Headers.Add("Cache-Control", "max-age=0");
            myHttpWebRequest.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            myHttpWebRequest.KeepAlive = true;
            myHttpWebRequest.Referer = "http://metanit.com/";
            try
            {
                myHttpWebResponse = (System.Net.HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (System.Net.WebException wex)
            {
                return new Info { error = wex.Message, isFind = false };
            }
            catch (System.UriFormatException ufe)
            {
                return new Info { error = ufe.Message, isFind = false };
            }
            catch (System.Exception ex)
            {
                throw new NotImplementedException();
            }
            return new Info
            {
                str = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream()).ReadToEnd(),
                isFind = true
            };
        }

        private static string MathcAddress(string hostName, string pattern, int number)
        {
            System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Match match = regEx.Match(hostName);

            return match.Groups[number].Value;
        }
        private static List<string> SearchPattern(string target, string pattern, int num = 0)
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
        /// <summary>
        /// check in the existence of the site
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        private static List<string> SiteIPAddress(string hostName)
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
        private static System.Net.IPHostEntry PingSite(string hostName)
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
        public static List<string> Ping(string hostName)
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
        private static string PingInfo(string p)
        {

            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();

            options.DontFragment = true;

            string data = new string('a',32);
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            System.Net.NetworkInformation.PingReply reply = pingSender.Send(p, timeout, buffer, options);

            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                return string.Format("\t Round Trip time: {1} ms, Time to live: {2}, Buffer size: {4}",reply.Address.ToString(),reply.RoundtripTime,reply.Options.Ttl,reply.Options.DontFragment,reply.Buffer.Length);
            }
            return "";
        }
    }
}