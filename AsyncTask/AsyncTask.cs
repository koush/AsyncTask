using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using async = System.Collections.IEnumerator;

namespace AsyncTask
{
    internal class State<T>
    {
        public State(ManualResetEvent ev)
        {
            Event = ev;
            OriginatingThread = Thread.CurrentThread;
        }

        public Exception Exception
        {
            get;
            set;
        }
        public ManualResetEvent Event
        {
            get;
            set;
        }
        public T Result
        {
            get;
            set;
        }
        public Thread OriginatingThread
        {
            get;
            set;
        }
        public AsyncTask<T> Task
        {
            get;
            set;
        }
    }
    
    public class AsyncTask<T> : Task<T>
	{
        void Continue(Task previous)
        {
            if (previous != null && previous.Exception != null)
            {
                mState.Exception = previous.Exception;
                mState.Event.Set();
            }

            try
            {
                if (!mEnumerator.MoveNext())
                {
                    if (previous == null)
                        mState.Exception = new Exception("Task ended without a result.");
                    else
                    {
                        Task<T> final = previous as Task<T>;
                        if (final != null)
                            mState.Result = final.Result;
                        else
                            mState.Exception = new Exception(String.Format("Task ended with a result that was not of Type {0}.", typeof(T)));
                    }
                    mState.Event.Set();
                    return;
                }
            }
            catch (Exception ex)
            {
                mState.Exception = ex;
                mState.Event.Set();
                return;
            }

            object o = mEnumerator.Current;
            Task t = o as Task;
            if (t != null)
            {
                t.ContinueWith(Continue);
                if (mState.OriginatingThread == Thread.CurrentThread)
                    t.Start();
                else
                    t.RunSynchronously();
            }
            else
            {
                mState.Result = (T)o;
                mState.Event.Set();
            }
        }
        
        static T Do(async tasks, State<T> state)
		{
            state.Task.Continue(null);
            state.Event.WaitOne();
            if (state.Exception != null)
                throw state.Exception;
            return state.Result;
		}

		async mEnumerator;
        State<T> mState;
        AsyncTask(async tasks, State<T> state)
            : base(delegate { return Do(tasks, state); })
		{
			mEnumerator = tasks;
            mState = state;
            mState.Task = this;
		}

        public AsyncTask(async tasks)
            : this(tasks, new State<T>(new ManualResetEvent(false)))
        {
        }
	}

    public static class TaskHelper
	{
        public static Task<string> DownloadStringTask(this WebClient client, String url)
        {
            return new Task<string>(delegate
            {
                return client.DownloadString(url);
            });
        }

        public static Task<T> Create<S,T>(Func<S, T> func, S s)
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
