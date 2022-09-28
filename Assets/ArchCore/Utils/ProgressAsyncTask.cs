

namespace ArchCore.Utils
{
    public class ProgressAsyncTask<T> : AsyncTask<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress">a value from 0 to 1</param>
        public delegate void TaskProgressDelegate(float progress);

        private TaskProgressDelegate onProgress;

        public ProgressAsyncTask<T> OnProgress(TaskProgressDelegate onProgress)
        {
            this.onProgress += onProgress;
            return this;
        }

        public void Progress(float progress)
        {
            if (onProgress != null)
                onProgress(progress);
        }

        public new ProgressAsyncTask<T> OnSuccess(TaskSuccessDelegate onSuccess)
        {
            base.OnSuccess(onSuccess);
            return this;
        }

        public new ProgressAsyncTask<T> OnComplete(TaskCompleteDelegate onComplete)
        {
            base.OnComplete(onComplete);
            return this;
        }
        
        public override void Dispose()
        {
            base.Dispose();
            onProgress = null;
        }
    }

    public class ProgressAsyncTask : AsyncTask
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="progress">a value from 0 to 1</param>
        public delegate void TaskProgressDelegate(float progress);

        private TaskProgressDelegate onProgress;

        public AsyncTask OnProgress(TaskProgressDelegate onProgress)
        {
            this.onProgress += onProgress;
            return this;
        }

        public void Progress(float progress)
        {
            if (onProgress != null)
                onProgress(progress);
        }

        public new ProgressAsyncTask OnSuccess(TaskSuccessDelegate onSuccess)
        {
            base.OnSuccess(onSuccess);
            return this;
        }

        public new ProgressAsyncTask OnComplete(TaskCompleteDelegate onComplete)
        {
            base.OnComplete(onComplete);
            return this;
        }

        public override void Dispose()
        {
            base.Dispose();
            onProgress = null;
        }
    }
}