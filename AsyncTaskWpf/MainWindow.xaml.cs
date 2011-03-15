using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;
using System.Threading.Tasks;
using System.Net;
using AsyncTask;
using async = System.Collections.IEnumerator;
using System.Text.RegularExpressions;

namespace AsyncTaskWpf
{
    public class DispatcherAsyncTask : Task, IContinueWithTask
    {
        Dispatcher mDispatcher;
        public DispatcherAsyncTask(Dispatcher dispatcher)
            : base(delegate{})
        {
            mDispatcher = dispatcher;
            ContinueWith(Continue);
        }

        void Continue(Task t)
        {
			mDispatcher.BeginInvoke(mAction, t);
        }

		Action<Task> mAction;
		Action<Task> IContinueWithTask.ContinueWith
		{
			get
			{
				return mAction; 
			}
			set
			{
				mAction = value;
			}
		}
    }

    public static class Extensions
    {
        public static Task Dispatcher(this DispatcherObject o)
        {
            return new DispatcherAsyncTask(o.Dispatcher);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        async DispatcherTest()
        {
            // download the google home page
            WebClient client = new WebClient();
            var google = client.DownloadStringTask("http://www.google.com");
            yield return google;
            // find and download the first link on the google home page
            Regex regex = new Regex("href=\"(?<href>http://.*?)\"");
            var match = regex.Match(google.Result);
            var firstHref = match.Groups["href"].Value;
            var firstHrefTask = TaskHelper.Create(client.DownloadString, firstHref);
            yield return firstHrefTask;

            // let's yield back onto the Dispatcher to get onto the UI thread
            yield return this.Dispatcher();

            textBox1.Text = firstHrefTask.Result;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
			var test1 = DispatcherTest().Async();
            test1.Start();
        }
    }
}
