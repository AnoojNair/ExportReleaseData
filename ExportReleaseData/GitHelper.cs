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
    }
}
