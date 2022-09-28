using System.Collections;
using UnityEngine;

namespace ArchCore.Utils
{
    public class ReusableYieldTask : CustomYieldInstruction
    {
        private IEnumerator task;
        private Coroutine coroutine;
        private Coroutine subCoroutine;

        private bool isKeepWaiting = true;
        public override bool keepWaiting => isKeepWaiting;

        public ReusableYieldTask(IEnumerator task)
        {
            this.task = task;
        }

        public ReusableYieldTask SetTask(IEnumerator task)
        {
            this.task = task;
            return this;
        }

        public ReusableYieldTask Start()
        {
            Stop();
            isKeepWaiting = true;
            coroutine = CoroutineRunner.RunCoroutine(CompleteCoroutine(task));
            return this;
        }

        private IEnumerator CompleteCoroutine(IEnumerator coroutine)
        {
            subCoroutine = CoroutineRunner.RunCoroutine(coroutine);
            yield return subCoroutine;
            isKeepWaiting = false;
        }

        public void Stop()
        {
            if (!coroutine.UnityNullCheck())
            {
                CoroutineRunner.Stop(coroutine);
            }

            if (!subCoroutine.UnityNullCheck())
            {
                CoroutineRunner.Stop(subCoroutine);
            }

            coroutine = null;
            subCoroutine = null;

            isKeepWaiting = false;
        }
    }
}