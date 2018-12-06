using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class Release
    {
        public int Id { get; set; }

        public string ProjectName { get; set; }

        public string Version { get; set; }

        public DateTime Created { get; set; }

        public int ProjectStars { get; set; }

        public int ProjectWatch{ get; set; }

        public int Forks { get; set; }
        public int NumberofPullRequests { get; set; }

        public int NumberOfFilesModified { get; set; }

        public int NumberOfAdditions { get; set; }

        public int NumberOfDeletions { get; set; }

        public List<int> Contributers { get; set; }

        public int NumberOfUniqueContributers { get; set; }

        public int NumberOfPRsReviewed { get; set; }

        public int NumberofReviewers { get; set; }

        public int NumberofReviews { get; set; }

        public int TestFilesChanged { get; set; }

        public int CommentsCount { get; set; }

        public int IntegrationBugsCount { get; set; }

        // Has to be the median 
        public int ContrubtionsFromMembers { get; set; }

        public bool IsLegacy { get; set; }

        public bool ExternalIntegration { get; set; }

        public bool Dependencies { get; set; }

        public List<PullRequest> PullRequests { get; set; }

        public int BugsCount { get; set; }

        public int TestCoverageScore { get; set; }

        public int ReleaseOrder { get; set; }

        public int PriorContributions { get; set; }

        public int TotalFollowers { get; set; }

        public int TotalContributers { get; set; }

        public override string ToString()
        {
            return String.Format($"{this.Id},{this.ProjectName},{this.Version},{this.ReleaseOrder},{this.Created},{this.ProjectStars},{this.ProjectWatch},{this.Forks},{this.TotalContributers},{this.NumberofPullRequests},{this.NumberOfFilesModified}," +
                $"{this.NumberOfAdditions},{this.NumberOfDeletions},{this.NumberOfPRsReviewed}," +
                $"{this.NumberOfUniqueContributers},{this.NumberofReviewers},{this.NumberofReviews}," +
                $"{this.TestFilesChanged},{this.PriorContributions},{this.TotalFollowers},{this.CommentsCount},{this.IntegrationBugsCount},{this.ContrubtionsFromMembers},{this.Dependencies},{this.TestCoverageScore},{this.BugsCount}");
        }


    }
}
