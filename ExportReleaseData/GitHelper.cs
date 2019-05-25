using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class GitHelper
    {
        public string GetOwnerNameFromGitUrl(string gitUrl)
        {
            return gitUrl.Trim().Split('/')[gitUrl.Trim().Split('/').Length - 2];
        }

        public string GetRepoNameFromGitUrl(string gitUrl)
        {
            return gitUrl.Trim().Split('/').LastOrDefault();
        }

        public List<Issue> ExtractAllBugs(Repository repository)
        {
            var allBugs = new List<Issue>();
            var all = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.All
            };
            var issues = repository.Client.Issue.GetAllForRepository(repository.Owner, repository.Name, all).Result;
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

        public List<Release> GetAllReleasesUsingReleaseTags(Repository repository,
            string patternUrl, string testPattern)
        {
            var index = 0;
            var allReleases = new List<Release>();
            var allUsers = repository.Client.Repository.Statistics.GetContributors(repository.Owner, repository.Name).Result;
            var tags = repository.Client.Repository.GetAllTags(repository.Owner, repository.Name).Result;
            // Go through all the release tags and extract commits and pull requests data
            foreach (var tag in tags)
            {
                try
                {
                    Octokit.Release release = null;

                    release = repository.Client.Repository.Release.Get(repository.Owner, repository.Name, tag.Name).Result;
                    var rel = new Release();
                    rel.ProjectName = repository.Owner + "-" + repository.Name;
                    rel.Contributers = new List<int>();
                    GetCommitInformation(release, rel, repository, tag.Commit.Sha);
                    if (index > 0 && index < tags.Count - 2)
                    {
                        rel = ExtractCommits(repository, tags[index - 1].Name, tag.Name, rel,
                            testPattern, allUsers.ToList());
                    }
                    rel.Id = release.Id;
                    rel.PullRequests = new List<PullRequest>();
                    (DateTime created, int pullRequests) = new HtmlParser().
                        ParseReleaseHtmlToCountPullRequests(patternUrl, release);
                    rel.Created = created;
                    rel.NumberofPullRequests = pullRequests;
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

        private void GetCommitInformation(Octokit.Release release, Release rel, Repository repository, string sha)
        {
            var commit = repository.Client.Repository.Commit.Get(repository.Owner, repository.Name, sha).Result;
            rel.NumberOfAdditions += commit.Stats.Additions;
            rel.NumberOfDeletions += commit.Stats.Deletions;
            rel.NumberOfFilesModified += commit.Files.Count;
            if (commit.Author != null)
            {
                rel.Contributers.Add(commit.Author.Id);
            }
        }

        private Release ExtractCommits(Repository repository,
            string currentTag, string previousTag, Release rel, string testPattern, List<Contributor> users)
        {
            var @base = previousTag;
            var head = currentTag;
            var commits = new List<GitHubCommit>();
            var response = repository.Client.Repository.Commit.Compare(repository.Owner, repository.Name,
                @base, head).Result;
            rel.NumberofPullRequests = response.Commits.Count;
            var allIds = new List<int>();
            Parallel.ForEach(response.Commits, (c) =>
            {
                GitHubCommit cmt = null;
                try
                {
                    cmt = repository.Client.Repository.Commit.Get(repository.Owner,
                        repository.Name, c.Sha).Result;
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
    }
}
