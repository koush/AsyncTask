using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

using async = System.Collections.IEnumerator;

namespace ConsoleApplication3
{
	class Program
	{
		static async Do()
		{
			WebClient client = new WebClient();
			var yahoo = client.DownloadStringTask("http://www.yahoo.com");
			yield return yahoo;
			var google = client.DownloadStringTask("http://www.google.com");
			yield return google;
			yield return google.Result + yahoo.Result;
		}

		static void Main(string[] args)
		{
			Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
			var stuff = Do().Await<string>();
			stuff.Wait();
		}
	}
}
