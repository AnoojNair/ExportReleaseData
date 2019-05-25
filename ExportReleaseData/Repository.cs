using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class Repository
    {
        public GitHubClient Client { get; set; }

        public string Owner { get; set; }

        public string Name { get; set; }
    }
}
