using System;
using System.Collections;
using UnityEngine;

namespace ArchCore.Utils
{
    public class ScheduleTask : YieldTask
    {
        public ScheduleTask(Action task, float timer) : base(DelayedTask(task, timer))
        {
        }

        private static IEnumerator DelayedTask(Action task, float timer)
        {
            yield return new WaitForSeconds(timer);

            task?.Invoke();
        }
    }
}