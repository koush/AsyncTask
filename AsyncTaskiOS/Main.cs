
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Text.RegularExpressions;

using async = System.Collections.IEnumerator;
using System.Net;
using AsyncTask;

namespace AsyncTaskiOS
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}
	
	class UIThreadTask : AsyncTask.AsyncTask
	{
		protected override void StartAsync()
		{
			Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
			
			mObject.BeginInvokeOnMainThread(delegate
			{
				OnCompleted();
			});
		}

		public UIThreadTask(NSObject o)
		{
			mObject = o;
		}

		NSObject mObject;
	}

	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
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
			// iOS does not actually need to be invoked onto a UI thread to modify the title.
			// This is just a test.
			mButton.SetTitle(firstHrefTask.Result.Substring(0, 10), MonoTouch.UIKit.UIControlState.Normal);
		}

		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			// If you have defined a view, add it here:
			// window.AddSubview (navigationController.View);
			
			window.MakeKeyAndVisible ();
			
			
			mButton.TouchDown += delegate
			{
				Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
				Test().Yield();
				Console.WriteLine("hello");
			};
			return true;
		}

		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}

