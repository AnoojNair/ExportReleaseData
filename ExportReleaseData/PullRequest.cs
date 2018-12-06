using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class PullRequest
    {
        public int ReleaseId { get; set; }

        public string ReleaseVersion { get; set; }

        public int PullId { get; set; }

        public int NumberOfFilesModified { get; set; }

        public int NumberOfAdditions { get; set; }

        public int NumberOfDeletions { get; set; }

        public int UserId { get; set; }

        public int ContributionsPrior { get; set; }

        public int Comments { get; set; }

        public int Reviewers { get; set; }

        public int TestCoverageScore { get; set; }

        // public int ReviewerContributions { get; set; }

        public int NumberOfReviews { get; set; }

        public int TestFilesChanged { get; set; }

        public override string ToString()
        {
            return String.Format($"{this.ReleaseId},{this.ReleaseVersion},{this.PullId}, {this.UserId}, {this.NumberOfFilesModified}, {this.NumberOfAdditions}, {this.NumberOfDeletions}, {this.ContributionsPrior},{ this.Comments}, { this.Reviewers}, { this.TestCoverageScore}, { this.NumberOfReviews}, { this.TestFilesChanged}");
        }

    }
}
