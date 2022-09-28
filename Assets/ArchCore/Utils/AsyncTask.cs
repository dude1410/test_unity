using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Utils
{
	public class AsyncTask : CustomYieldInstruction, IDisposable
	{

		public static AsyncTask All(params AsyncTask[] tasks)
		{
			return new CombinedAsyncTask(new List<AsyncTask>(tasks));
		}

		public delegate void TaskSuccessDelegate();

		public delegate void TaskFailDelegate(Exception error);

		public delegate void TaskCompleteDelegate(Exception error);

		public bool IsDone { get; private set; }
		public Exception Error { get; private set; }

		private TaskSuccessDelegate onSuccess;
		private TaskFailDelegate onFail;
		private TaskCompleteDelegate onComplete;
		private bool _keepWaiting = true;
		
		public override bool keepWaiting => _keepWaiting;

		public AsyncTask OnSuccess(TaskSuccessDelegate onSuccess)
		{
			this.onSuccess += onSuccess;
			if (IsDone && Error == null)
			{
				onSuccess?.Invoke();
			}

			return this;
		}

		public AsyncTask OnFail(TaskFailDelegate onFail)
		{
			this.onFail += onFail;
			if (IsDone && Error != null)
			{
				onFail?.Invoke(Error);
			}

			return this;
		}

		public AsyncTask OnComplete(TaskCompleteDelegate onComplete)
		{
			this.onComplete += onComplete;
			if (IsDone)
			{
				onComplete?.Invoke(Error);
			}

			return this;
		}

		public virtual void Success()
		{
			IsDone = true;

			onSuccess?.Invoke();

			onComplete?.Invoke(null);

			_keepWaiting = false;
		}

		public void Fail(Exception error)
		{
			Error = error;
			IsDone = true;

			onFail?.Invoke(error);

			onComplete?.Invoke(error);

			_keepWaiting = false;
		}

		public AsyncTask RegisterForDispose(IDisposablesContainer disposablesContainer)
		{
			disposablesContainer.RegisterForDispose(this);

			return this;
		}

		public virtual void Dispose()
		{
			onSuccess = null;
			onComplete = null;
			onFail = null;
		}
	}

	internal class CombinedAsyncTask : AsyncTask
	{
		private List<AsyncTask> tasks;
		private bool failed;
		private Exception error;

		public CombinedAsyncTask(List<AsyncTask> tasks)
		{
			this.tasks = tasks;
			ListenAllSuccess();
		}

		private void ListenAllSuccess()
		{
			tasks.RemoveAll(t => t.IsDone);
			
			foreach (var task in tasks)
			{
				AsyncTask tempTask = task;
				task
					.OnSuccess(delegate
					{
						tasks.Remove(tempTask);
						CheckComplete();
					})
					.OnFail(delegate(Exception error)
					{
						tasks.Remove(tempTask);
						failed = true;
						this.error = error;
						CheckComplete();
					});
			}
		}

		private void CheckComplete()
		{
			if (tasks.Count > 0)
			{
				return;
			}

			if (failed)
			{
				Fail(error);
			}
			else
			{
				Success();
			}
		}
	}
}