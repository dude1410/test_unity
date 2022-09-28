using System.Collections;
using UnityEngine;

namespace ArchCore.Utils
{
    public class YieldTask : AsyncTask
    {
        private readonly IEnumerator task;


        private Coroutine coroutine, sub_coroutine;

        public YieldTask(IEnumerator task)
        {
            this.task = task;
        }

        public YieldTask Start()
        {
            coroutine = CoroutineRunner.RunCoroutine(CompleteCoroutine(task));
            return this;
        }

        private IEnumerator CompleteCoroutine(IEnumerator cor)
        {
            sub_coroutine = CoroutineRunner.RunCoroutine(cor);
            yield return sub_coroutine;
            Success();
        }

        public void Stop()
        {
            if (coroutine != null)
                CoroutineRunner.Stop(coroutine);
            if (sub_coroutine != null)
                CoroutineRunner.Stop(sub_coroutine);

            coroutine = null;
            sub_coroutine = null;
            
            Success();
        }
    }
}