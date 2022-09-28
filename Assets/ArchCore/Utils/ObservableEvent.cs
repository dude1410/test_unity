using System;

namespace ArchCore.Utils
{
    public class ObservableEventToken
    {
        private Action call;

        public ObservableEventToken(Action call)
        {
            this.call = call;
        }
        
        public void Call()
        {
            call();
        }
    }
    
    public class ObservableEvent
    {
        private Action singleSubscriptions;
        private Action permanentSubscriptions;

        public ObservableEvent(out ObservableEventToken token)
        {
            token = new ObservableEventToken(Call);
        }

        void Call()
        {
            singleSubscriptions?.Invoke();
            permanentSubscriptions?.Invoke();
            singleSubscriptions = null;
        }

        public void SingleSubscribe(Action call)
        {
            singleSubscriptions += call;
        }
        
        public void PermanentSubscribe(Action call)
        {
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
        
        public static ObservableEvent operator +(ObservableEvent lhs, Action rhs)
        {
            lhs.PermanentSubscribe(rhs);
            return lhs;
        }
        
        public static ObservableEvent operator *(ObservableEvent lhs, Action rhs)
        {
            lhs.SingleSubscribe(rhs);
            return lhs;
        }
        
        public static ObservableEvent operator -(ObservableEvent lhs, Action rhs)
        {
            lhs.RemovePermanentSubscription(rhs);
            return lhs;
        }
        
        public static ObservableEvent operator /(ObservableEvent lhs, Action rhs)
        {
            lhs.RemoveSingleSubscription(rhs);
            return lhs;
        }
    }
}