using System;
using UnityEngine;
using Zenject;

namespace ArchCore.Utils
{
    [NoReflectionBaking]
    public class SwitchObservableEventToken<T>
    {
        private Action<T> call;

        public SwitchObservableEventToken(Action<T> call)
        {
            this.call = call;
        }

        public void Call(T value)
        {
            call(value);
        }
    }

    [NoReflectionBaking]
    public class SwitchObservableEvent<T> : CustomYieldInstruction
    {
        private Action<T> singleSubscriptions;
        private Action<T> permanentSubscriptions;
        private T valueT;
        public override bool keepWaiting => valueT == null;
        public T Value => valueT;
        public SwitchObservableEvent(out SwitchObservableEventToken<T> token)
        {
            token = new SwitchObservableEventToken<T>(Call);
        }

        void Call(T value)
        {
            valueT = value;

            if (valueT != null)
            {
                singleSubscriptions?.Invoke(valueT);
                permanentSubscriptions?.Invoke(valueT);
                singleSubscriptions = null;
            }
        }

        public void SingleSubscribe(Action<T> call)
        {
            if (valueT != null)
                call(valueT);
            else
                singleSubscriptions += call;
        }

        public void PermanentSubscribe(Action<T> call)
        {
            if (valueT != null)
                call(valueT);

            permanentSubscriptions += call;
        }

        public void RemoveSingleSubscription(Action<T> call)
        {
            singleSubscriptions -= call;
        }

        public void RemovePermanentSubscription(Action<T> call)
        {
            permanentSubscriptions -= call;
        }

        public static SwitchObservableEvent<T> operator +(SwitchObservableEvent<T> lhs, Action<T> rhs)
        {
            lhs.PermanentSubscribe(rhs);
            return lhs;
        }

        public static SwitchObservableEvent<T> operator *(SwitchObservableEvent<T> lhs, Action<T> rhs)
        {
            lhs.SingleSubscribe(rhs);
            return lhs;
        }

        public static SwitchObservableEvent<T> operator -(SwitchObservableEvent<T> lhs, Action<T> rhs)
        {
            lhs.RemovePermanentSubscription(rhs);
            return lhs;
        }

        public static SwitchObservableEvent<T> operator /(SwitchObservableEvent<T> lhs, Action<T> rhs)
        {
            lhs.RemoveSingleSubscription(rhs);
            return lhs;
        }
    }
}