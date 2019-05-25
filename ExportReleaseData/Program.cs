using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExportReleaseData
{
    class Program
    {

        static void Main(string[] args)
        {
            var client = new GitHubClient(new ProductHeaderValue("Extract-PR"));


            var inputHelper = new InputHelper();
            string userName = inputHelper.GetGitHubUserName();
            string pwd = inputHelper.GetGitHubPassword();
            string folderLocation = inputHelper.GetLocationToExportData();
            string gitUrl = inputHelper.GetGitUrlToExport();
            string testPattern = inputHelper.GetTestFilesLocation();

            var authHelper = new AuthenticationHelper();
            Credentials basicAuth = authHelper.GenerateCredentialsToAuthenticate(userName, pwd);
            client.Credentials = basicAuth;

            var gitHelper = new GitHelper();
            string owner = gitHelper.GetOwnerNameFromGitUrl(gitUrl);
            string name = gitHelper.GetRepoNameFromGitUrl(gitUrl);

            var bugs = ExtractAllBugs(client, owner, name);
            ExportBugsData(bugs, folderLocation, name);

            var releases = ExtractReleaseData(client, owner, name, gitUrl + "/pull/", testPattern, bugs);
            ExportReleaseToCSV(releases, folderLocation, name);
            
           
        }

        private static void ExportReleaseToCSV(List<Release> releases, string folderLocation, string name)
        {
            var releaseCsv = new StringBuilder();
            releaseCsv.AppendLine("Id,ProjectName,Version,Release Order,Created,ProjectStars," +
                "ProjectWatch,Forks,TotalContributers,NumberofPullRequests,NumberOfFilesModified,NumberOfAdditions,NumberOfDeletions,NumberOfPRsReviewed,NumberOfUniqueContributers,NumberofReviewers,NumberofReviews,TestFilesChanged,PriorContributions,TotalFollowers,CommentsCount,IntegrationBugsCount,ContrubtionsFromMembers,Dependencies,TestCoverageScore,BugsCount");
            foreach (var release in releases)
            {
                releaseCsv.AppendLine(release.ToString());
            }
            File.WriteAllText(folderLocation + "\\" + name + "Releases.csv", releaseCsv.ToString());
        }

        private static void ExportBugsData(List<Issue> bugs, string folderLocation, string name)
        {
            var issueCsv = new StringBuilder();
            Console.WriteLine("Extract All Issues...");
            issueCsv.AppendLine("IssueId,Title,CreatedDate");
            foreach (var bug in bugs)
            {
                issueCsv.AppendLine(new Issue { Id = bug.Id, Title = bug.Title, Created = bug.Created }.ToString());
            }

            File.WriteAllText(folderLocation + "\\" + name + "Issues.csv", bugs.ToString());
        }

        private static Release ExtractCommits(GitHubClient client, string owner, string name, 
            string currentTag, string previousTag, Release rel, string testPattern, List<Contributor> users)
        {
            var @base = previousTag;
            var head = currentTag;
            var commits = new List<GitHubCommit>();
            var response = client.Repository.Commit.Compare(owner, name, @base, head).Result;
            rel.NumberofPullRequests = response.Commits.Count;
            var allIds = new List<int>();
            Parallel.ForEach(response.Commits, (c) =>
            {
                GitHubCommit cmt = null;
                try
                {
                    cmt = client.Repository.Commit.Get(owner, name, c.Sha).Result;
                    Console.WriteLine("Commit Id: " + cmt.NodeId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to fetch commit : " + ex.Message);
                }
                if (cmt != null)
                {
                    rel.NumberOfAdditions += cmt.Stats.Additions;
                    rel.NumberOfDeletions += cmt.Stats.Deletions;
                    rel.NumberOfFilesModified += cmt.Files.Count;
                    rel.TestFilesChanged += cmt.Files.Count(f => f.BlobUrl.ToLowerInvariant().Contains(testPattern));
                    rel.CommentsCount += cmt.Commit.CommentCount;
                    if (cmt.Author != null)
                    {
                        allIds.Add(cmt.Author.Id);
                        rel.PriorContributions += users.FirstOrDefault(u => u.Author.Id == cmt.Author.Id) != null ?
                        users.FirstOrDefault(u => u.Author.Id == cmt.Author.Id).Total : 0;
                        //  rel.TotalFollowers += client.User. et(cmt.Author.Login).Result.Followers;
                    }
                }
            });
            rel.NumberOfUniqueContributers = allIds.Distinct().Count();
            rel.TestCoverageScore = Math.Abs(rel.NumberOfFilesModified - rel.TestFilesChanged);
            return rel;
        }

        

        private static List<Release> ExtractReleaseData(GitHubClient client, string owner, string name, 
            string patternUrl, string testPattern, List<Issue> allBugs)
        {
            
            List<Release> allReleases = GetAllReleasesUsingReleaseTags(client, owner, name, patternUrl, testPattern);

            // Group releases by month and year into a dictionary
            SortedList<string, List<Release>> dictReleases = GroupAllReleasesByMonth(allReleases);

            // Add up the release information which are close to each other.
            return CombineReleaseAndCorrespondingBugsInfo(allBugs, dictReleases);
        }

        private static List<Release> CombineReleaseAndCorrespondingBugsInfo(List<Issue> allBugs, SortedList<string, List<Release>> dictReleases)
        {
            var releases = new List<Release>();
            var j = 0;
            var keys = dictReleases.Keys.OrderBy(k => Convert.ToInt32(k.Split('-')[1])).ToArray();
            foreach (var item in dictReleases.OrderBy(d => Convert.ToInt32(d.Key.Split('-')[1])))
            {
                var release = new Release();
                release.Created = item.Value[0].Created;
                release.Version = item.Value[0].Version;
                release.ReleaseOrder = j + 1;
                foreach (var rel in item.Value)
                {
                    AddCloseReleases(release, rel);
                }
                if (j < dictReleases.Count - 1)
                {
                    release.BugsCount = allBugs.Count(b => b.Created < dictReleases[keys[j + 1]][0].Created && b.Created >= release.Created);
                }
                else
                {
                    release.BugsCount = allBugs.Count(b => b.Created < DateTime.Now && b.Created >= release.Created);
                }
                releases.Add(release);
                j++;
            }
            return releases;
        }

        private static List<Release> GetAllReleasesUsingReleaseTags(GitHubClient client, string owner, string name, string patternUrl, string testPattern)
        {
            var index = 0;
            var allReleases = new List<Release>();
            var allUsers = client.Repository.Statistics.GetContributors(owner, name).Result;
            var tags = client.Repository.GetAllTags(owner, name).Result;
            // Go through all the release tags and extract commits and pull requests data
            foreach (var tag in tags)
            {
                try
                {
                    Octokit.Release release = null;

                    release = client.Repository.Release.Get(owner, name, tag.Name).Result;
                    var rel = new Release();
                    rel.ProjectName = owner + "-" + name;
                    rel.Contributers = new List<int>();
                    GetCommitInformation(release, rel, owner, name, client, tag.Commit.Sha);
                    if (index > 0 && index < tags.Count - 2)
                    {
                        rel = ExtractCommits(client, owner, name, tags[index - 1].Name, tag.Name, rel,
                            testPattern, allUsers.ToList());
                    }
                    rel.Id = release.Id;
                    rel.PullRequests = new List<PullRequest>();
                    ParseReleaseHtmlToCountPullRequests(patternUrl, release, rel);
                    rel.Version = release.TagName;

                    allReleases.Add(rel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                index++;
            }

            return allReleases;
        }

        private static SortedList<string, List<Release>> GroupAllReleasesByMonth(List<Release> allReleases)
        {
            var dictReleases = new SortedList<string, List<Release>>();
            foreach (var rel in allReleases)
            {
                if (!dictReleases.Keys.Contains(rel.Created.ToString("MM-yyyy")))
                {
                    dictReleases.Add(rel.Created.ToString("MM-yyyy"), new List<Release> { rel });
                }
                else
                {
                    var tempReleases = dictReleases[rel.Created.ToString("MM-yyyy")];
                    tempReleases.Add(rel);
                    dictReleases[rel.Created.ToString("MM-yyyy")] = tempReleases.OrderBy(r => r.Created).ToList();
                }
            }

            return dictReleases;
        }

        /// <summary>
        /// Counts the number of pull requests from the Release html body using html parsing
        /// </summary>
        /// <param name="patternUrl"></param>
        /// <param name="release"></param>
        /// <param name="rel"></param>
        private static void ParseReleaseHtmlToCountPullRequests(string patternUrl, Octokit.Release release, 
            Release rel)
        {
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
                    
                    rel.Created = ScrapCreatedDate(input);
                    var m = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                    if (m.Count > 0)
                    {
                        Parallel.ForEach(m.OfType<Match>(), (match) =>
                        {
                            var pullUrl = input.Substring(match.Index, input.IndexOf('"', match.Index + 6) - match.Index);
                            var pullId = Convert.ToInt32(!pullUrl.Split('/')[6].Contains('#') ? pullUrl.Split('/')[6] : pullUrl.Split('/')[6].Split('#')[0]);
                            Console.WriteLine(pullId);
                            rel.NumberofPullRequests += 1;
                        });
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static void AddCloseReleases(Release release, Release rel)
        {
            release.CommentsCount += rel.CommentsCount;
            release.ContrubtionsFromMembers += rel.ContrubtionsFromMembers;
            release.NumberOfAdditions += rel.NumberOfAdditions;
            release.NumberOfDeletions += rel.NumberOfDeletions;
            release.NumberOfFilesModified += rel.NumberOfFilesModified;
            release.NumberOfPRsReviewed += rel.NumberOfPRsReviewed;
            release.NumberofPullRequests += rel.NumberofPullRequests;
            release.NumberofReviewers += rel.NumberofReviewers;
            release.NumberofReviews += rel.NumberofReviews;
            release.TestFilesChanged += rel.TestFilesChanged;
            release.TestCoverageScore += rel.TestCoverageScore;
            release.NumberOfUniqueContributers += rel.NumberOfUniqueContributers;
            release.PriorContributions += rel.PriorContributions;
            release.TotalFollowers += rel.TotalFollowers;
        }

        private static void GetCommitInformation(Octokit.Release release, Release rel, string owner,
            string name, GitHubClient client, string sha)
        {
            var commit = client.Repository.Commit.Get(owner, name, sha).Result;
            rel.NumberOfAdditions += commit.Stats.Additions;
            rel.NumberOfDeletions += commit.Stats.Deletions;
            rel.NumberOfFilesModified += commit.Files.Count;
            if (commit.Author != null)
            {
                rel.Contributers.Add(commit.Author.Id);
            }
        }

        private static DateTime ScrapCreatedDate(string input)
        {
            var firstIndex = input.LastIndexOf("<relative-time datetime=") + "<relative-time datetime =".Length;
            var endIndex = input.IndexOf("title=", firstIndex);
            var strDate = input.Substring(firstIndex, endIndex - firstIndex).Remove('"');
            var arrDate = strDate.Split('-');
            return new DateTime(Convert.ToInt32(arrDate[0]), Convert.ToInt32(arrDate[1]), Convert.ToInt32(arrDate[2].Split('T')[0]));
        }

        private static List<Issue> ExtractAllBugs(GitHubClient client, string owner, string name)
        {
            var allBugs = new List<Issue>();
            var all = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.All
            };
            var issues = client.Issue.GetAllForRepository(owner, name, all).Result;
            Console.WriteLine("Total Issues: " + issues);
            foreach (var issue in issues)
            {
                if (issue.Labels.Any(l => l.Name.ToLowerInvariant().Contains("bug")))
                {
                    allBugs.Add(new Issue { Id = issue.Id, Title = issue.Title, Created = issue.CreatedAt.UtcDateTime });                   
                }
            }
            Console.WriteLine("Total Bugs found: " + allBugs.Count);
            return allBugs;
        }
    }
}
