using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using async = System.Collections.IEnumerator;

namespace AsyncTask
{
    internal class StateBase<T>
    {
        public Exception Exception
        {
            get;
            set;
        }
        public T Result
        {
            get;
            set;
        }
    }

    internal class State<T> : StateBase<T>
    {
        public State(ManualResetEvent ev)
        {
            Event = ev;
            OriginatingThread = Thread.CurrentThread;
        }

        public ManualResetEvent Event
        {
            get;
            set;
        }

        public AsyncMethodTask<T> Task
        {
            get;
            set;
        }

        public Thread OriginatingThread
        {
            get;
            set;
        }
    }

    public class AsyncTaskScheduler : TaskScheduler
    {
        static AsyncTaskScheduler mScheduler = new AsyncTaskScheduler();

        internal static AsyncTaskScheduler Instance
        {
            get
            {
                return mScheduler;
            }
        }

		static bool mNeedsHack = Environment.OSVersion.Platform != PlatformID.Win32NT && Environment.OSVersion.Platform != PlatformID.Win32S && Environment.OSVersion.Platform != PlatformID.Win32Windows && Environment.OSVersion.Platform != PlatformID.WinCE && Environment.OSVersion.Platform != PlatformID.Xbox;
        static System.Reflection.MethodInfo mExecute = typeof(Task).GetMethod("Execute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance);

        bool TryExecuteTaskHack(Task task)
        {
            if (task.IsCompleted)
                return false;

            if (task.Status == TaskStatus.WaitingToRun)
            {
                mExecute.Invoke(task, new object[] { null });
                return true;
            }

            return false;
        }

        internal void Run(Task t)
        {
            if (mExecute != null && mNeedsHack)
            {
                TryExecuteTaskHack(t);
            }
            else
            {
                TryExecuteTask(t);
            }
        }

        protected override void QueueTask(Task task)
        {
			Console.WriteLine("Queueing {0} from {1}.", task, Thread.CurrentThread.ManagedThreadId);
            var atask = task as IAsyncTask;

            // see if this is a async task or the async continuation task
            if (atask == null)
                Run(task);
            else
                atask.Start(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }
    }

    public interface IAsyncTask
    {
        Action<Task> Start
        {
            get;
        }
    }

    public class AsyncTask : AsyncTask<object>
    {
        public AsyncTask(Action<Task> start)
            : base(start)
        {
        }

        public void OnCompleted()
        {
            OnCompleted(null);
        }
    }

    public class AsyncTask<T> : Task<T>, IAsyncTask
    {
        static T Do(StateBase<T> state)
        {
            if (state.Exception != null)
                throw state.Exception;
            return state.Result;
        }

        StateBase<T> mState;
        AsyncTask(StateBase<T> state)
            : base(delegate { return Do(state); })
        {
            mState = state;
        }

        public AsyncTask(Action<Task> action)
            : this(new StateBase<T>())
        {
            mStart = action;
        }

        protected void OnCompleted(T result)
        {
            mState.Result = result;
            OnFinished();
        }

        protected void OnException(Exception ex)
        {
            mState.Exception = ex;
            OnFinished();
        }

        void OnFinished()
        {
            AsyncTaskScheduler.Instance.Run(this);
        }

        Action<Task> mStart;
        Action<Task> IAsyncTask.Start
        {
            get
            {
                return mStart;
            }
        }
    }

    internal class AsyncMethodTask : AsyncMethodTask<object>
    {
        public AsyncMethodTask(async tasks)
            : base(tasks)
        {
        }
    }

    internal class AsyncMethodTask<T> : AsyncTask<T>
    {
        public void Continue(Task previous)
        {
            if (previous != null && previous.Exception != null)
            {
				OnException(previous.Exception);
				return;
            }

            try
            {
                if (!mEnumerator.MoveNext())
                {
					if (previous == null)
						OnException(new Exception("Task ended without a result."));
					else
					{
						Task<T> final = previous as Task<T>;
						if (final != null)
							OnCompleted(final.Result);
						else if (GetType() != typeof(AsyncMethodTask))
							OnException(new Exception(String.Format("Task ended with a result that was not of Type {0}.", typeof(T))));
					}
                    return;
                }
            }
            catch (Exception ex)
            {
				OnException(ex);
                return;
            }

            object o = mEnumerator.Current;
            Task t = o as Task;
            if (t != null)
            {
                IAsyncTask atask = t as IAsyncTask;
                if (atask != null)
                {
                    t.ContinueWith(Continue, AsyncTaskScheduler.Instance);
                    t.Start(AsyncTaskScheduler.Instance);
                }
                else
                {
                    t.ContinueWith(Continue);
                    t.Start();
                }
            }
            else
            {
				OnCompleted((T)o);
            }
        }

		static void Start(Task t)
		{
			AsyncMethodTask<T> self = t as AsyncMethodTask<T>;
			self.Continue(null);
		}

        async mEnumerator;
        internal AsyncMethodTask(async tasks)
            : base(Start)
        {
            mEnumerator = tasks;
        }
    }

    class WebClientDownloadStringTask : AsyncTask<string>
    {
        static void Start(Task task)
        {
            var self = task as WebClientDownloadStringTask;
            DownloadStringCompletedEventHandler handler = null;
            handler = (e, a) =>
            {
                self.mClient.DownloadStringCompleted -= handler;
                if (a.Error == null)
                    self.OnCompleted(a.Result);
                else
                    self.OnException(a.Error);
            };
            self.mClient.DownloadStringCompleted += handler;
            self.mClient.DownloadStringAsync(new Uri(self.mUrl));
        }

        public WebClientDownloadStringTask(WebClient client, string url)
            : base(Start)
        {
            mClient = client;
            mUrl = url;
        }

        string mUrl;
        WebClient mClient;
    }

    public static class TaskHelper
    {
        public static Task<T> Yield<T>(this async async)
        {
			var ret = new AsyncMethodTask<T>(async);
			ret.Start(AsyncTaskScheduler.Instance);
			//ret.Start();
            return ret;
        }

        public static Task Yield(this async async)
        {
            var ret = new AsyncMethodTask(async);
			ret.Start(AsyncTaskScheduler.Instance);
			//ret.Start();
			return ret;
        }

        /*
                public static Task<string> DownloadStringTask(this WebClient client, String url)
                {
                    return new Task<string>(delegate
                    {
                        return client.DownloadString(url);
                    });
                }
                */

        public static Task<string> DownloadStringTask(this WebClient client, String url)
        {
            return new WebClientDownloadStringTask(client, url);
        }

        public static Task<T> Create<S, T>(Func<S, T> func, S s)
        {
            return new Task<T>(delegate { return func(s); });
        }

        public static Task<T> Create<R, S, T>(Func<R, S, T> func, R r, S s)
        {
            return new Task<T>(delegate { return func(r, s); });
        }

        public static Task<T> Create<Q, R, S, T>(Func<Q, R, S, T> func, Q q, R r, S s)
        {
            return new Task<T>(delegate { return func(q, r, s); });
        }

    }
}
