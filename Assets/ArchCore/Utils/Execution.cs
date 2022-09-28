using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArchCore.Utils.Conditions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ArchCore.Utils.Executions
{
    public abstract class BaseExecution : CustomYieldInstruction
    {
        private class NullExe : BaseExecution
        {
            public override bool keepWaiting => false;
        }

        private class NeverExe : BaseExecution
        {
            public override bool keepWaiting => true;
        }

        static BaseExecution()
        {
            Null = new NullExe();
            Never = new NeverExe();
        }

        public static BaseExecution Null { get; }
        public static BaseExecution Never { get; }

        public event Action OnExecutionEnd;

        public override bool keepWaiting => _keepWaiting;
        public bool IsExecuted { get; private set; } = false;

        private bool _keepWaiting;
        protected YieldTask task;

        protected BaseExecution()
        {
            _keepWaiting = true;
        }

        public BaseExecution Execute()
        {
            if (IsExecuted) return this;
            IsExecuted = true;
            task = new YieldTask(ExecutionProcess()).Start();
            return this;
        }

        protected virtual IEnumerator ExecutionProcess()
        {
            yield break;
        }

        public virtual bool Finish()
        {
            Stop();
            return false;
        }

        public virtual void Stop()
        {
            task?.Stop();
            task = null;
            _keepWaiting = false;
        }

        protected void Complete()
        {
            _keepWaiting = false;
            OnExecutionEnd?.Invoke();
        }


        public static Execution operator >(BaseExecution lhs, BaseExecution rhs)
        {
            if (lhs is Execution re && !re.IsExecuted)
            {
                re.Add(rhs);
                return re;
            }

            return new Execution(lhs, rhs);
        }

        public static Execution operator <(BaseExecution lhs, BaseExecution rhs)
        {
            if (rhs is Execution re && !re.IsExecuted)
            {
                re.AddFirst(rhs);
                return re;
            }

            return new Execution(lhs, rhs);
        }

        public static OrPassAsyncExecution operator |(BaseExecution lhs, BaseExecution rhs)
        {
            if (lhs is OrPassAsyncExecution re && !re.IsExecuted)
            {
                re.Add(rhs);
                return re;
            }

            if (rhs is OrPassAsyncExecution le && !le.IsExecuted)
            {
                le.Add(rhs);
                return le;
            }

            return new OrPassAsyncExecution(lhs, rhs);
        }

        public static AsyncExecution operator &(BaseExecution lhs, BaseExecution rhs)
        {
            if (lhs is AsyncExecution re && !re.IsExecuted)
            {
                re.Add(rhs);
                return re;
            }

            if (rhs is AsyncExecution le && !le.IsExecuted)
            {
                le.Add(rhs);
                return le;
            }

            return new AsyncExecution(lhs, rhs);
        }
    }

    public abstract class GroupExecution : BaseExecution
    {
        protected readonly List<BaseExecution> executions;

        protected GroupExecution()
        {
            executions = new List<BaseExecution>();
        }

        protected GroupExecution(params BaseExecution[] executions)
        {
            this.executions = executions.ToList();
        }

        protected GroupExecution(IEnumerable<BaseExecution> executions)
        {
            this.executions = executions.ToList();
        }

        public void Add(BaseExecution execution)
        {
            executions.Add(execution);
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var exe in executions)
            {
                exe.Stop();
            }
        }

        public override bool Finish()
        {
            bool canFinish = true;
            foreach (var exe in executions)
            {
                if (!exe.Finish())
                {
                    canFinish = false;
                }
            }

            //base.Stop();
            return canFinish;
        }
    }

    public class Execution : GroupExecution
    {
        public Execution()
        {
        }

        public Execution(params BaseExecution[] executions) : base(executions)
        {
        }

        public void AddFirst(BaseExecution execution)
        {
            if (executions.Count == 0)
            {
                executions.Add(execution);
                return;
            }

            executions.Insert(0, execution);
        }

        public Execution(IEnumerable<BaseExecution> executions) : base(executions)
        {
        }

        protected override IEnumerator ExecutionProcess()
        {
            foreach (var execution in executions)
            {
                execution.Execute();
                if (execution.keepWaiting)
                    yield return execution;
            }

            Complete();
        }

        public void StopSequence()
        {
            executions.Clear();
            base.Stop();
        }

    }

    public class AsyncExecution : GroupExecution
    {
        public AsyncExecution()
        {
        }

        public AsyncExecution(params BaseExecution[] executions) : base(executions)
        {
        }

        public AsyncExecution(IEnumerable<BaseExecution> executions) : base(executions)
        {
        }

        protected override IEnumerator ExecutionProcess()
        {
            foreach (var execution in executions)
            {
                execution.Execute();
            }

            foreach (var execution in executions)
            {
                if (execution.keepWaiting)
                    yield return execution;
            }

            Complete();
        }
    }

    public class OrPassAsyncExecution : GroupExecution
    {
        public OrPassAsyncExecution()
        {
        }

        public OrPassAsyncExecution(params BaseExecution[] executions) : base(executions)
        {
        }

        public OrPassAsyncExecution(IEnumerable<BaseExecution> executions) : base(executions)
        {
        }

        protected override IEnumerator ExecutionProcess()
        {
            foreach (var execution in executions)
            {
                execution.OnExecutionEnd += Complete;
                execution.Execute();
            }

            yield break;
        }
    }

    public class CoroutineExe : BaseExecution
    {
        private readonly IEnumerator coroutine;
        private readonly Action endState;

        public CoroutineExe(IEnumerator coroutine)
        {
            this.coroutine = coroutine;
        }

        public CoroutineExe(IEnumerator coroutine, Action endState)
        {
            this.coroutine = coroutine;
            this.endState = endState;
        }

        private YieldTask iTask;

        protected override IEnumerator ExecutionProcess()
        {
            iTask = new YieldTask(coroutine);
            yield return iTask.Start();

            Complete();
        }

        public override void Stop()
        {
            base.Stop();
            iTask?.Stop();
        }

        public override bool Finish()
        {
            Stop();
            if (endState == null) return false;
            endState();
            return true;
        }
    }

    public class WaitExe : BaseExecution
    {
        private float time;
        private float startTime;

        private bool isPaused = false;

        public WaitExe(float time)
        {
            this.time = time;
        }

        public override bool Finish()
        {
            Stop();
            return true;
        }

        protected override IEnumerator ExecutionProcess()
        {
            startTime = Time.time;
            yield return new WaitForSeconds(time);
            Complete();
        }

        public void Pause()
        {
            if (isPaused) return;

            time = Time.time - startTime;

            task?.Stop();
            isPaused = true;
        }

        public void Resume()
        {
            if (!isPaused) return;
            isPaused = false;
            task = new YieldTask(ExecutionProcess());
        }
    }

    public class WaitExeRealTime : BaseExecution
    {
        private float time;
        private float startTime;

        public WaitExeRealTime(float time)
        {
            this.time = time;
        }
        
        public override bool Finish()
        {
            Stop();
            return true;
        }
        
        protected override IEnumerator ExecutionProcess()
        {
            startTime = Time.realtimeSinceStartup;
            while (startTime + time > Time.realtimeSinceStartup)
            {
                yield return null;
            }
            Complete();
        }
    }

    public class WaitFrameExe : BaseExecution
    {
        private readonly int frameCount;

        private bool isPaused = false;
        private int framesPassed = 0;

        public WaitFrameExe(int frameCount = 1)
        {
            this.frameCount = frameCount;
        }

        public override bool Finish()
        {
            Stop();
            return true;
        }

        protected override IEnumerator ExecutionProcess()
        {
            for (int i = 0; i < frameCount - framesPassed; i++)
            {
                yield return new WaitForFixedUpdate();
                framesPassed++;
            }

            Complete();
        }

        public void Pause()
        {
            if (isPaused) return;


            task?.Stop();
            isPaused = true;
        }

        public void Resume()
        {
            if (!isPaused) return;
            isPaused = false;
            task = new YieldTask(ExecutionProcess());
        }
    }

    public class YieldExecution : BaseExecution
    {
        private YieldInstruction yieldInstruction;

        public YieldExecution(YieldInstruction yieldInstruction = null)
        {
            this.yieldInstruction = yieldInstruction;
        }

        protected override IEnumerator ExecutionProcess()
        {
            yield return yieldInstruction;
            Complete();
        }
    }

    public class EventExe : BaseExecution
    {
        public interface IKey
        {
            void Invoke();
        }

        private class Key : IKey
        {
            private Action _event;

            public void OnComplete(Action call)
            {
                _event += call;
            }

            public void Invoke()
            {
                _event?.Invoke();
            }

        }

        public EventExe(out IKey iKey)
        {
            var key = new Key();
            key.OnComplete(Complete);
            iKey = key;
        }
    }

    public class WaitForUnityEventExe : BaseExecution
    {
        private UnityEvent _event;

        public WaitForUnityEventExe(UnityEvent _event)
        {
            this._event = _event;
            _event.AddListener(EventComplete);
        }

        private void EventComplete()
        {
            Clear();
            Complete();
        }

        private bool cleared = false;

        public void Clear()
        {
            if (!cleared) return;
            cleared = true;
            _event.RemoveListener(EventComplete);
        }
    }

    public class WaitTapExe : BaseExecution
    {
        public override bool Finish()
        {
            Stop();
            return true;
        }

        protected override IEnumerator ExecutionProcess()
        {
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
            Complete();
        }
    }

    public class UnityEvent1<T> : UnityEvent<T>
    {
    }

    public class UnityEvent2<T0, T1> : UnityEvent<T0, T1>
    {
    }

    public class WaitForUnityEventExe<T0> : BaseExecution
    {
        public T0 Result => data;
        private UnityEvent1<T0> _event;

        public WaitForUnityEventExe(UnityEvent1<T0> _event)
        {
            this._event = _event;
            _event.AddListener(EventComplete);
        }

        private void EventComplete(T0 t)
        {
            data = t;
            Clear();
            Complete();
        }

        private bool cleared = false;
        private T0 data;

        public void Clear()
        {
            if (!cleared) return;
            cleared = true;
            _event.RemoveListener(EventComplete);
        }
    }

    public class ConditionalEventExe<T> : BaseExecution
    {
        public T Result => data;

        private readonly UnityEvent1<T> targetEvent;
        private readonly Condition<T> passCondition;
        private T data;

        public ConditionalEventExe(UnityEvent1<T> targetEvent, Condition<T> passCondition)
        {
            this.targetEvent = targetEvent;
            this.passCondition = passCondition;
        }

        protected override IEnumerator ExecutionProcess()
        {
            targetEvent.AddListener(SelectTile);
            do
            {
                yield return new WaitForUnityEventExe<T>(targetEvent).Execute();
            } while (!passCondition.Check(data));

            Clear();

            Complete();
        }

        private void SelectTile(T arg)
        {
            data = arg;
        }

        private bool cleared = false;

        public void Clear()
        {
            if (!cleared) return;
            cleared = true;
            targetEvent.RemoveListener(SelectTile);
        }
    }

    public class TweenExe : BaseExecution
    {
        private readonly Func<Tween> tween;
        private Tween tweener;

        public TweenExe(Func<Tween> tween)
        {
            this.tween = tween;
        }

        protected override IEnumerator ExecutionProcess()
        {
            tweener = tween?.Invoke().OnComplete(Complete);
            yield break;
        }

        public override bool Finish()
        {
            tweener.Complete(true);
            return true;
        }

        public override void Stop()
        {
            tweener.Kill();
            base.Stop();
        }
    }

    public class ActionExe : BaseExecution
    {
        private readonly Action action;
        private bool invoked = false;

        public ActionExe(Action action)
        {
            this.action = action;
        }

        protected override IEnumerator ExecutionProcess()
        {
            if (!invoked)
            {
                invoked = true;
                action?.Invoke();
            }

            Complete();
            yield break;
        }

        public override bool Finish()
        {
            if (keepWaiting && !invoked)
            {
                invoked = true;
                action?.Invoke();
            }

            return true;
        }
    }

    public class ProcessExe : BaseExecution
    {
        private readonly Func<BaseExecution> action;
        private BaseExecution exe;

        public ProcessExe(Func<BaseExecution> action)
        {
            this.action = action;
        }

        protected override IEnumerator ExecutionProcess()
        {
            exe = action?.Invoke();
            if (exe != null && exe.keepWaiting)
            {
                yield return exe.Execute();
            }

            Complete();
        }

        public override void Stop()
        {
            base.Stop();
            exe?.Stop();
        }
    }

    public class AsyncTaskExe : BaseExecution
    {
        private readonly AsyncTask task;

        public AsyncTaskExe(AsyncTask task)
        {
            this.task = task;
        }

        protected override IEnumerator ExecutionProcess()
        {
            yield return task;
            Complete();
        }
    }

    public class AsyncTaskProcessExe : BaseExecution
    {
        private readonly Func<AsyncTask> task;

        public AsyncTaskProcessExe(Func<AsyncTask> task)
        {
            this.task = task;
        }

        protected override IEnumerator ExecutionProcess()
        {
            yield return task();
            Complete();
        }
    }

    public class IteratorExe : BaseExecution
    {
        private readonly Action task;
        private readonly YieldInstruction interval;
        private readonly bool startWithWait;

        public IteratorExe(Action task, YieldInstruction interval, bool startWithWait = false)
        {
            this.task = task;
            this.interval = interval;
            this.startWithWait = startWithWait;
        }

        protected override IEnumerator ExecutionProcess()
        {
            if (startWithWait) yield return interval;
            while (true)
            {
                task?.Invoke();
                yield return interval;
            }

            Complete();
        }

    }

}