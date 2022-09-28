using System;
using UnityEngine;

namespace ArchCore.Utils
{
    public class SwitchObservableEventToken
    {
        private Action<bool> call;

        public SwitchObservableEventToken(Action<bool> call)
        {
            this.call = call;
        }
        
        public void Call(bool value)
        {
            call(value);
        }
    }
    
    public class SwitchObservableEvent : CustomYieldInstruction
    {
        private Action singleSubscriptions;
        private Action permanentSubscriptions;
        
        public bool Value { get; private set; }
        public override bool keepWaiting => !Value;
        public SwitchObservableEvent(out SwitchObservableEventToken token)
        {
            token = new SwitchObservableEventToken(Call);
        }

        void Call(bool value)
        {
            Value = value;

            if (Value)
            {
                singleSubscriptions?.Invoke();
                permanentSubscriptions?.Invoke();
                singleSubscriptions = null;
            }
        }

        public void SingleSubscribe(Action call)
        {
            if (Value)
                call();
            else
                singleSubscriptions += call;
        }
        
        public void PermanentSubscribe(Action call)
        {
            if (Value)
                call();
            permanentSubscriptions += call;
        }
        
        public void RemoveSingleSubscription(Action call)
        {
            singleSubscriptions -= call;
        }
        
        public void RemovePermanentSubscription(Action call)
        {
            permanentSubscriptions -= call;
        }
        
        public static SwitchObservableEvent operator +(SwitchObservableEvent lhs, Action rhs)
        {
            lhs.PermanentSubscribe(rhs);
            return lhs;
        }
        
        public static SwitchObservableEvent operator *(SwitchObservableEvent lhs, Action rhs)
        {
            lhs.SingleSubscribe(rhs);
            return lhs;
        }
        
        public static SwitchObservableEvent operator -(SwitchObservableEvent lhs, Action rhs)
        {
            lhs.RemovePermanentSubscription(rhs);
            return lhs;
        }
        
        public static SwitchObservableEvent operator /(SwitchObservableEvent lhs, Action rhs)
        {
            lhs.RemoveSingleSubscription(rhs);
            return lhs;
        }

       
    }
}