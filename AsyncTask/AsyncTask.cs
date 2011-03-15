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

    public interface IContinueWithTask
    {
        Action<Task> ContinueWith
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

        internal void Run(Task t)
        {
            TryExecuteTask(t);
        }

        protected override void QueueTask(Task task)
        {
            IAsyncTask atask = (IAsyncTask)task;
            atask.Action();
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
        Action Action
        {
            get;
            set;
        }


    }

    public class AsyncTask<T> : Task<T>, IAsyncTask, IContinueWithTask
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
            ContinueWith(Continue);
        }

        void Continue(Task t)
        {
            ThreadPool.QueueUserWorkItem(o =>
                {
                    mAction(t);
                });
        }

        public AsyncTask(Action action)
            : this(new StateBase<T>())
        {
            (this as IAsyncTask).Action = action;
        }

        public void OnCompleted(T result)
        {
            mState.Result = result;
            OnFinished();
        }

        public void OnException(Exception ex)
        {
            mState.Exception = ex;
            OnFinished();
        }

        void OnFinished()
        {
            AsyncTaskScheduler.Instance.Run(this);
            //AsyncTaskScheduler.mQueue.Enqueue(this);
            //AsyncTaskScheduler.mSemaphore.Release();
        }

        Action IAsyncTask.Action
        {
            get;
            set;
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

    internal class AsyncMethodTask : AsyncMethodTask<object>
    {
        public AsyncMethodTask(async tasks)
            : base(tasks, new State<object>(new ManualResetEvent(false)))
        {
        }
    }
    
    internal class AsyncMethodTask<T> : Task<T>
    {
        public virtual void Continue(Task previous)
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
                        else if (GetType() != typeof(AsyncMethodTask))
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
                IContinueWithTask c = t as IContinueWithTask;
                if (c != null)
                    c.ContinueWith = Continue;
                else
                    t.ContinueWith(Continue);
                IAsyncTask atask = t as IAsyncTask;
                if (atask != null)
                    t.Start(AsyncTaskScheduler.Instance);
                else if (mState.OriginatingThread == Thread.CurrentThread)
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
        internal AsyncMethodTask(async tasks, State<T> state)
            : base(delegate { return Do(tasks, state); })
        {
            mEnumerator = tasks;
            mState = state;
            mState.Task = this;
        }

        public AsyncMethodTask(async tasks)
            : this(tasks, new State<T>(new ManualResetEvent(false)))
        {
        }
    }

    public static class TaskHelper
    {
		public static Task<T> Async<T>(this async async)
		{
			return new AsyncMethodTask<T>(async);
		}

		public static Task Async(this async async)
		{
			return new AsyncMethodTask(async);
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
            AsyncTask<string> ret = new AsyncTask<string>(delegate
            {
                client.DownloadStringAsync(new Uri(url));
            });
            DownloadStringCompletedEventHandler handler = null;
            handler = (sender, args) =>
                {
                    Console.WriteLine(url + " done!");
                    client.DownloadStringCompleted -= handler;
                    if (args.Error != null)
                        ret.OnException(args.Error);
                    else
                        ret.OnCompleted(args.Result);
                };

            client.DownloadStringCompleted += handler;
            return ret;
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
