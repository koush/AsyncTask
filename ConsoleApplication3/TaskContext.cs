using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using async = System.Collections.IEnumerator;

namespace ConsoleApplication3
{
	public class AsyncTask<T> : Task<T>
	{
		static T Do(async tasks, Mutex mutex)
		{
			
			return default(T);
		}

		async mEnumerator;
		Mutex mMutex = new Mutex();
		Exception mException;
		public AsyncTask(async tasks, Mutex mutex)
			: base(delegate { return Do(tasks, mutex })
		{
			mEnumerator = tasks;
			mMutex = mutex;
		}
		
		void Continue()
		{
			if (mException != null || !mEnumerator.MoveNext())
			{
				mAction();
			}
			else
			{
				// we have yielded a result, so let's trigger the action to trigger the next result.
				mEnumerator.Current();
			}
		}

		internal void Start()
		{

		}
	}

	public class AsyncTaskScheduler : TaskScheduler
	{
		protected override void QueueTask(Task task)
		{
		}

		protected override bool TryExecuteTaskInline(Task _task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			throw new NotImplementedException();
		}

		protected override bool TryDequeue(Task task)
		{
			return base.TryDequeue(task);
		}
	}

	public static class Extensions
	{
		static Extensions()
		{
			mScheduler = new AsyncTaskScheduler();
		}
		static AsyncTaskScheduler mScheduler;
		public static Task<T> Await<T>(this async enumerator)
		{
			var t = new AsyncTask<T>(enumerator);
			t.Start(mScheduler);
			return t;
		}
	}

	public class TaskContext
	{
		void Continue()
		{
			if (mException != null || !mEnumerator.MoveNext())
			{
				mAction();
			}
			else
			{
				// we have yielded a result, so let's trigger the action to trigger the next result.
				mEnumerator.Current();
			}
		}

		Action mCompletionHandler;
		public Action TaskCompletionHandler
		{
			get
			{
				return mCompletionHandler ?? (mCompletionHandler = new Action(Continue));
			}
		}

		IEnumerable<Action> mTasks;
		IEnumerator<Action> mEnumerator;
		Action mAction;
		public void Attach(IEnumerable<Action> tasks, Action a)
		{
			mAction = a;
			mTasks = tasks;
			mEnumerator = mTasks.GetEnumerator();
			Continue();
		}

		Exception mException;
		public void SetException(Exception e)
		{
			mException = e;
		}
	}
}
