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
    class Foo
    {
        public static Foo operator ++(Foo c1)
        {
            return c1;
        }
    }

	class Program
	{
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
            var test1 = new AsyncTask<string>(Test1());
            test1.Start();

            var test2 = new AsyncTask<string>(Test2());
            test2.Start();
            
            Console.WriteLine();

            var test3 = new AsyncTask<string>(Test3());
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
