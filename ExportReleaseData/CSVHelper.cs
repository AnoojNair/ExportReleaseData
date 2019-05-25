using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class CSVHelper
    {
        public void ExportReleaseToCSV(List<Release> releases, string folderLocation, string name)
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

        public void ExportBugsData(List<Issue> bugs, string folderLocation, string name)
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
    }
}
