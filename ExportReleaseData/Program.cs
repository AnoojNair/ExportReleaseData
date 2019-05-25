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

            var inputHelper = new InputHelper();
            string userName = inputHelper.GetGitHubUserName();
            string pwd = inputHelper.GetGitHubPassword();
            string folderLocation = inputHelper.GetLocationToExportData();
            string gitUrl = inputHelper.GetGitUrlToExport();
            string testPattern = inputHelper.GetTestFilesLocation();


            var gitHelper = new GitHelper();
            var repository = new Repository();
            repository.Client = new GitHubClient(new ProductHeaderValue("Extract-PR"));
            repository.Owner = gitHelper.GetOwnerNameFromGitUrl(gitUrl);
            repository.Name = gitHelper.GetRepoNameFromGitUrl(gitUrl);      

            var authHelper = new AuthenticationHelper();
            Credentials basicAuth = authHelper.GenerateCredentialsToAuthenticate(userName, pwd);
            repository.Client.Credentials = basicAuth;

            var csvHelper = new CSVHelper();

            var bugs = gitHelper.ExtractAllBugs(repository);
            csvHelper.ExportBugsData(bugs, folderLocation, repository.Name);

            var releases = ExtractReleaseData(repository, gitUrl + "/pull/", testPattern, 
                bugs, gitHelper);
            csvHelper.ExportReleaseToCSV(releases, folderLocation, repository.Name);          
        }
         
        private static List<Release> ExtractReleaseData(Repository repository, 
            string patternUrl, string testPattern, List<Issue> allBugs, GitHelper gitHelper)
        {
            
            List<Release> allReleases = gitHelper.GetAllReleasesUsingReleaseTags(repository, patternUrl, 
                testPattern);

            // Group releases by month and year into a dictionary
            SortedList<string, List<Release>> dictReleases = GroupAllReleasesByMonth(allReleases);

            // Add up the release information which are close to each other.
            return CombineReleaseAndCorrespondingBugsInfo(allBugs, dictReleases);
        }

        private static List<Release> CombineReleaseAndCorrespondingBugsInfo(List<Issue> allBugs,
            SortedList<string, List<Release>> dictReleases)
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
                    release += rel;
                }
                if (j < dictReleases.Count - 1)
                {
                    release.BugsCount = allBugs.Count(b => b.Created < dictReleases[keys[j + 1]][0].Created &&
                    b.Created >= release.Created);
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
    }
}
