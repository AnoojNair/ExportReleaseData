using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class Issue
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public DateTime Created { get; set; }

        public override string ToString()
        {
            return this.Id + "," + this.Title.Replace(",",String.Empty).Replace("\'",String.Empty).Replace("\"",String.Empty) + "," + this.Created;
        }
    }
}
