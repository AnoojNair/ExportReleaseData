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
            Console.WriteLine("Enter Github Username:");
            var userName = Console.ReadLine();
            Console.WriteLine("Enter Github password:");
            var pwd = GetPassword();
            var basicAuth = new Credentials(userName, pwd);
            client.Credentials = basicAuth;
            Console.WriteLine("Enter Folder location to export:");
            var folderLocation = Console.ReadLine();
            Console.WriteLine("Enter git url(foramt:https://github.com/vuejs/vue):");
            var gitUrl = Console.ReadLine();
            Console.WriteLine("Enter Test files location(foramt:/test/):");
            var testPattern = Console.ReadLine();
            var owner = gitUrl.Trim().Split('/')[gitUrl.Trim().Split('/').Length - 2];
            var name = gitUrl.Trim().Split('/').LastOrDefault();
            //var folderLocation = @"D:\ML\Pet Project\Data";
            var allVuePrs = ExtractPR(client, owner, name, gitUrl + "/pull/", testPattern);
            File.WriteAllText(folderLocation + "\\" + name + "PR.csv", allVuePrs[1].ToString());
            File.WriteAllText(folderLocation + "\\" + name + "Releases.csv", allVuePrs[0].ToString());
            File.WriteAllText(folderLocation + "\\" + name + "Issues.csv", allVuePrs[2].ToString());

        }

        private static Release ExtractCommits(GitHubClient client, string owner, string name, string currentTag, string previousTag, Release rel, string testPattern, List<Contributor> users)
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

        public static string GetPassword()
        {
            var pwd = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pwd += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
                    {
                        pwd = pwd.Substring(0, (pwd.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            return pwd;
        }

        private static List<StringBuilder> ExtractPR(GitHubClient client, string owner, string name, string patternUrl, string testPattern)
        {
            var prCsv = new StringBuilder();
            var releaseCsv = new StringBuilder();
            var issueCsv = new StringBuilder();
            var allReleases = new List<Release>();
            var allBugs = new List<Issue>();
            ExtractIssues(client, owner, name, issueCsv, allBugs);
            var allUsers = client.Repository.Statistics.GetContributors(owner, name).Result;
            var tags = client.Repository.GetAllTags(owner, name).Result;
            //   var releaseList = client.Repository.Release.GetAll(owner, name, new ApiOptions { PageSize = 500 }).Result;
            releaseCsv.AppendLine("Id,ProjectName,Version,Release Order,Created,ProjectStars,ProjectWatch,Forks,TotalContributers,NumberofPullRequests,NumberOfFilesModified,NumberOfAdditions,NumberOfDeletions,NumberOfPRsReviewed,NumberOfUniqueContributers,NumberofReviewers,NumberofReviews,TestFilesChanged,PriorContributions,TotalFollowers,CommentsCount,IntegrationBugsCount,ContrubtionsFromMembers,Dependencies,TestCoverageScore,BugsCount");
            prCsv.AppendLine("ReleaseId, Release Verson, Pull Id,UserId, NumberOfFilesModified, NumberOfAdditions, NumberOfDeletions, ContributionsPrior, Comments,  Reviewers,  TestCoverageScore,  NumberOfReviews,  TestFilesChanged");
            issueCsv.AppendLine("IssueId,Title,CreatedDate");
            var index = 0;
            foreach (var tag in tags)
            //   Parallel.ForEach(tags, (tag) =>
            {
                try
                {
                    Octokit.Release release = null;
                    //if (releaseList.Count > 0)
                    //{
                    //    release = client.Repository.Release.Get(owner, name, releaseList.FirstOrDefault(r => r.TagName == tag.Name).Id).Result;
                    //}
                    //else
                    //{

                    release = client.Repository.Release.Get(owner, name, tag.Name).Result;
                    // }
                    var htmlUrl = release.HtmlUrl;
                    Console.WriteLine(htmlUrl);
                    string html = string.Empty;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(htmlUrl);
                    request.Timeout = Timeout.Infinite;
                    request.KeepAlive = true;
                    request.AutomaticDecompression = DecompressionMethods.GZip;
                    var rel = new Release();
                    rel.ProjectName = owner + "-" + name;
                    rel.Forks = 0;
                    rel.ProjectWatch = 0;
                    rel.ProjectStars = 0;
                    rel.Contributers = new List<int>();
                    GetCommitInformation(release, rel, owner, name, client, tag.Commit.Sha);
                    if (index > 0 && index < tags.Count - 2)
                    {
                        rel = ExtractCommits(client, owner, name, tags[index - 1].Name, tag.Name, rel, testPattern, allUsers.ToList());
                    }
                    rel.Id = release.Id;
                    rel.PullRequests = new List<PullRequest>();
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            html = reader.ReadToEnd();
                            // find commit url from the html
                            string pattern = @"href=" + '\"' + patternUrl;
                            string input = html;
                            //      var nextReleaseDate = allReleases.Count == 0 ? DateTime.Now : allReleases[(int)index - 1].Created;
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
                                    //var pullRequest = client.Repository.PullRequest.Get(owner, name, pullId);
                                    //var pr = pullRequest.Result;
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    rel.Version = release.TagName;
                    // Get the total contributions from all the authors

                    allReleases.Add(rel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                index++;
            }

            var dictReleases = new SortedList<string, List<Release>>();
            var releases = new List<Release>();
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
                releaseCsv.AppendLine(release.ToString());
                j++;
            }
            return new List<StringBuilder> { releaseCsv, prCsv, issueCsv };
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

        private static void GetCommitInformation(Octokit.Release release, Release rel, string owner, string name, GitHubClient client, string sha)
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

        private static void ExtractIssues(GitHubClient client, string owner, string name, StringBuilder issueCsv, List<Issue> allBugs)
        {
            Console.WriteLine("Extract All Issues...");
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
                    issueCsv.AppendLine(new Issue { Id = issue.Id, Title = issue.Title, Created = issue.CreatedAt.UtcDateTime }.ToString());
                }
            }
            Console.WriteLine("Total Bugs found: " + allBugs.Count);
        }
    }
}
