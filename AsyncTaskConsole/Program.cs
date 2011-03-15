using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

using async = System.Collections.IEnumerator;

namespace AsyncTask
{
    class Program
    {
		// here's the callback spaghetti for a normal asynchronous workflow
		static void Test1WithoutAsync()
		{
			// download the google home page
			var google = new WebClient();
			google.DownloadStringCompleted += (e, r) =>
				{
					// find and download the first link on the google home page
					Regex regex = new Regex("href=\"(?<href>http://.*?)\"");
					var match = regex.Match(r.Result);
					var firstHref = match.Groups["href"].Value;

					var firstHrefClient = new WebClient();
					firstHrefClient.DownloadStringCompleted += (e2, r2) =>
						{
							Console.WriteLine(r2.Result);
						};
					firstHrefClient.DownloadStringAsync(new Uri(firstHref));
				};
			google.DownloadStringAsync(new Uri("http://www.google.com"));
		}

        static async Test1()
        {
            // download the google home page
            WebClient client = new WebClient();
            var google = client.DownloadStringTask("http://www.google.com");
            yield return google;
            // find and download the first link on the google home page
            Regex regex = new Regex("href=\"(?<href>http://.*?)\"");
            var match = regex.Match(google.Result);
            var firstHref = match.Groups["href"].Value;
            var firstHrefTask = client.DownloadStringTask(firstHref);
            yield return firstHrefTask;
        }

        static async Test2()
        {
            // download the microsoft home page
            WebClient client = new WebClient();
            var microsoft = client.DownloadStringTask("http://www.microsoft.com");
            yield return microsoft;
            // find and download the first link on the microsoft home page
            Regex regex = new Regex("href=\"(?<href>http://.*?)\"");
            var match = regex.Match(microsoft.Result);
            var firstHref = match.Groups["href"].Value;
            var firstHrefTask = client.DownloadStringTask(firstHref);
            yield return firstHrefTask;

            // the return doesn't need to be a task, for example, returning an object is completely
            // legitimate async result.
            // return the the first link found and an excerpt:
            yield return firstHref + ": " + firstHrefTask.Result.Substring(0, 100) + "...";
        }


        static async Test3()
        {
            WebClient client = new WebClient();
            var google = client.DownloadStringTask("http://www.google.com");
            yield return google;

            throw new Exception("Testing an exception");
        }

        static void Main(string[] args)
        {
			var test1 = Test1().Async<string>();
            test1.Start();

			var test2 = Test2().Async<string>();
            test2.Start();
            
            Console.WriteLine();

			var test3 = Test3().Async<string>();
            test3.Start();

            Task.WaitAll(test1, test2);

            Console.WriteLine(test1.Result);
            Console.WriteLine();
            Console.WriteLine(test2.Result);
            Console.WriteLine();
            try
            {
                Console.WriteLine(test3.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception:");
                Console.WriteLine(e);
            }

        }
    }
}
