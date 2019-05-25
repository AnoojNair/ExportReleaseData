using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportReleaseData
{
    public class InputHelper
    {
        public string GetGitHubUserName()
        {
            Console.WriteLine("Enter Github Username:");
            var userName = Console.ReadLine();
            return userName;
        }

        public string GetGitHubPassword()
        {
            Console.WriteLine("Enter Github password:");
            var pwd = GetPassword();
            return pwd;
        }

        public string GetLocationToExportData()
        {
            Console.WriteLine("Enter Folder location to export:");
            var folderLocation = Console.ReadLine();
            return folderLocation;
        }

        public string GetGitUrlToExport()
        {
            Console.WriteLine("Enter git url(foramt:https://github.com/vuejs/vue):");
            var gitUrl = Console.ReadLine();
            return gitUrl;
        }

        public string GetTestFilesLocation()
        {
            Console.WriteLine("Enter Test files location(foramt:/test/):");
            var testPattern = Console.ReadLine();
            return testPattern;
        }

        public string GetPassword()
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
    }
}
