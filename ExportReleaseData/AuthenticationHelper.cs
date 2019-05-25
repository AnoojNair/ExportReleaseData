using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class AuthenticationHelper
    {
        public Credentials GenerateCredentialsToAuthenticate(string userName, string pwd)
        {
            return new Credentials(userName, pwd);
        }
    }
}
