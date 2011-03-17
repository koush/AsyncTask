using System;
using System.Collections.Generic;

namespace System.Threading.Tasks
{
	// Summary:
	//     Represents the current stage in the lifecycle of a System.Threading.Tasks.Task.
	public enum TaskStatus
	{
		// Summary:
		//     The task has been initialized but has not yet been scheduled.
		Created = 0,
		//
		// Summary:
		//     The task is waiting to be activated and scheduled internally by the .NET
		//     Framework infrastructure.
		WaitingForActivation = 1,
		//
		// Summary:
		//     The task has been scheduled for execution but has not yet begun executing.
		WaitingToRun = 2,
		//
		// Summary:
		//     The task is running but has not yet completed.
		Running = 3,
		//
		// Summary:
		//     The task has finished executing and is implicitly waiting for attached child
		//     tasks to complete.
		WaitingForChildrenToComplete = 4,
		//
		// Summary:
		//     The task completed execution successfully.
		RanToCompletion = 5,
		//
		// Summary:
		//     The task acknowledged cancellation by throwing an OperationCanceledException
		//     with its own CancellationToken while the token was in signaled state, or
		//     the task's CancellationToken was already signaled before the task started
		//     executing.
		Canceled = 6,
		//
		// Summary:
		//     The task completed due to an unhandled exception.
		Faulted = 7,
	}
	
	public abstract class TaskScheduler
	{
        protected abstract void QueueTask(Task task);

        protected abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);

        protected abstract IEnumerable<Task> GetScheduledTasks();
		
		protected bool TryExecuteTask(Task task)
		{
			task.Execute();
			if (task.ContinuationTask != null && task.ContinuationTaskScheduler != null)
				task.ContinuationTaskScheduler.Start(task.ContinuationTask);
			return true;
		}
		
		public static TaskScheduler Default
		{
			get;
			internal set;
		}
		
		internal void Start(Task t)
		{
			QueueTask(t);
		}
	}
	
	class ThreadPoolScheduler : TaskScheduler
	{
		protected override void QueueTask (Task task)
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				TryExecuteTask(task);
			});
		}
		
		protected override IEnumerable<Task> GetScheduledTasks ()
		{
			throw new NotImplementedException ();
		}
		
		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			throw new NotImplementedException ();
		}
	}
	
	public class Task
	{
		public Boolean IsCompleted
		{
			get;
			internal set;
		}
		
		public TaskStatus Status
		{
			get;
			internal set;
		}
		
		public Task(Action action)
		{
			Action = action;
		}
		
		internal Action Action
		{
			get;
			set;
		}
		
		public Exception Exception
		{
			get;
			internal set;
		}
		
		internal void Execute()
		{
			try
			{
				Action();
			}
			catch (Exception ex)
			{
				Exception = ex;
			}
		}
		
		public void Start()
		{
			Start(TaskScheduler.Default);
		}
		
		public void Start(TaskScheduler scheduler)
		{
			scheduler.Start(this);
		}
		
		internal Task ContinuationTask
		{
			get;
			set;
		}
		
		internal TaskScheduler ContinuationTaskScheduler
		{
			get;
			set;
		}
		
		public Task ContinueWith(Action<Task> continuationAction)
		{
			return ContinueWith(continuationAction, TaskScheduler.Default);
		}
		
		public Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler)
		{
			ContinuationTaskScheduler = scheduler;
			return ContinuationTask = new Task(() => continuationAction(this));
		}
	}
	
	internal class ThisWrapper<T> 
	{
		public T This
		{
			get;
			set;
		}
	}
	
	public class Task<T> : Task
	{
		public Task(Func<T> func)
			: this(func, new ThisWrapper<Task<T>>())
		{
		}
		
		static void ActionWrapper(ThisWrapper<Task<T>> wrapper)
		{
			wrapper.This.Result = wrapper.This.mFunc();
		}
		
		Task(Func<T> func, ThisWrapper<Task<T>> wrapper)
			: base(delegate { ActionWrapper(wrapper); })
		{
			wrapper.This = this;
			mFunc = func;
		}
		
		public T Result
		{
			get;
			internal set;
		}
		
		Func<T> mFunc;
	}
}