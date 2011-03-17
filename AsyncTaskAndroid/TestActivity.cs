using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using AsyncTask;
using async = System.Collections.IEnumerator;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AsyncTaskAndroid
{
	class UIThreadTask : AsyncTask.AsyncTask
	{
		protected override void StartAsync()
		{
			Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
			Console.WriteLine(this);
			Console.WriteLine(mActivity);

			mActivity.RunOnUiThread(() =>
				{
					Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
					OnCompleted();
				});
			Console.WriteLine("Done!");
		}

		public UIThreadTask(Activity activity)
		{
			mActivity = activity;
		}

		Activity mActivity;
	}

	[Activity(Label = "AsyncTaskAndroid", MainLauncher = true)]
	public class TestActivity : Activity
	{
		async Test()
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
			
			yield return new UIThreadTask(this);
			button.Text = firstHrefTask.Result.Substring(0, 10);
		}

		Button button;
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			button = FindViewById<Button>(Resource.Id.MyButton);

			button.Click += new EventHandler(button_Click);
		}

		void button_Click(object sender, EventArgs e)
		{
			Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
			Test().Yield();
			Console.WriteLine("yielded.");
		}
	}
}

