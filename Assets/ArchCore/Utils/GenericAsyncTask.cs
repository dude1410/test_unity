using System;

namespace ArchCore.Utils
{
    public abstract class AsyncTask<TObject, TTask> : AsyncTask where TTask : AsyncTask
    {
        public new delegate void TaskSuccessDelegate(TObject result);

        public new delegate void TaskCompleteDelegate(TObject result, Exception error);

        public TObject Result { get; private set; }

        private TaskSuccessDelegate onSuccess;
        private TaskCompleteDelegate onComplete;

        public TTask OnSuccess(TaskSuccessDelegate onSuccess)
        {
            this.onSuccess += onSuccess;
            if (IsDone && Error == null)
            {
                onSuccess?.Invoke(Result);
            }

            return this as TTask;
        }

        public TTask OnComplete(TaskCompleteDelegate onComplete)
        {
            this.onComplete += onComplete;
            if (IsDone)
            {
                onComplete?.Invoke(Result, Error);
            }

            return this as TTask;
        }

        public void Success(TObject result)
        {
            Result = result;

            onSuccess?.Invoke(result);

            onComplete?.Invoke(result, null);

            base.Success();
        }

        public override void Success()
        {
            throw new Exception("Calling Success without result on AsyncTask that expects " + typeof(TObject));
        }
    }

    public class AsyncTask<T> : AsyncTask<T, AsyncTask<T>>
    {
    }
}