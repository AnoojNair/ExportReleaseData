using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class HtmlParser
    {
        public (DateTime created, int pullRequest) ParseReleaseHtmlToCountPullRequests(string patternUrl, Octokit.Release release)
        {
            var created = DateTime.MinValue;
            var pullRequests = 0;
            try
            {
                var htmlUrl = release.HtmlUrl;
                Console.WriteLine(htmlUrl);
                string html = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(htmlUrl);
                request.Timeout = Timeout.Infinite;
                request.KeepAlive = true;
                request.AutomaticDecompression = DecompressionMethods.GZip;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                    // find commit url from the html
                    string pattern = @"href=" + '\"' + patternUrl;
                    string input = html;

                    created = ScrapCreatedDate(input);
                    var m = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                    
                    if (m.Count > 0)
                    {
                        Parallel.ForEach(m.OfType<Match>(), (match) =>
                        {
                            var pullUrl = input.Substring(match.Index, input.IndexOf('"', match.Index + 6) - match.Index);
                            var pullId = Convert.ToInt32(!pullUrl.Split('/')[6].Contains('#') ? pullUrl.Split('/')[6] : pullUrl.Split('/')[6].Split('#')[0]);
                            Console.WriteLine(pullId);
                            // rel.NumberofPullRequests += 1;
                            pullRequests++;
                        });
                    }
                }                
            }
            catch (Exception ex)
            {

            }
            return (created, pullRequests);
        }

        private DateTime ScrapCreatedDate(string input)
        {
            var firstIndex = input.LastIndexOf("<relative-time datetime=") + "<relative-time datetime =".Length;
            var endIndex = input.IndexOf("title=", firstIndex);
            var strDate = input.Substring(firstIndex, endIndex - firstIndex).Remove('"');
            var arrDate = strDate.Split('-');
            return new DateTime(Convert.ToInt32(arrDate[0]), Convert.ToInt32(arrDate[1]), Convert.ToInt32(arrDate[2].Split('T')[0]));
        }
    }
}
