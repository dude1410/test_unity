using System;
using Zenject;

namespace ArchCore.Utils
{
    //Disable Reflection backing for this class, because current version of Zenject cant bake this class
    [NoReflectionBaking]
    public class ObserveDataToken<TType>
    {
        private readonly Action<TType> set;

        public ObserveDataToken(Action<TType> set)
        {
            this.set = set;
        }

        public void Set(TType data)
        {
            set?.Invoke(data);
        }
    }

    //Disable Reflection backing for this class, because current version of Zenject cant bake this class
    [NoReflectionBaking]
    public class ObservableData<TType>
    {
        public delegate void OnChangeHandler(TType data);

        private TType data;
       
        private OnChangeHandler singleSubscriptions;
        private OnChangeHandler permanentSubscriptions;

        public ObservableData(out ObserveDataToken<TType> token)
        {
            token = new ObserveDataToken<TType>(SetData);
        }

        void SetData(TType newData)
        {
            Data = newData;
        }
        public TType Data
        {
            get => data;
            private set
            {
                if (!Equals(data, value))
                {
                    data = value;
                    singleSubscriptions?.Invoke(value);
                    permanentSubscriptions?.Invoke(value);
                    singleSubscriptions = null;
                }
            }
        }

        public void SingleSubscribe(OnChangeHandler call)
        {
            singleSubscriptions += call;
        }
        
        public void PermanentSubscribe(OnChangeHandler call)
        {
            permanentSubscriptions += call;
        }
        
        public void RemoveSingleSubscription(OnChangeHandler call)
        {
            singleSubscriptions -= call;
        }
        
        public void RemovePermanentSubscription(OnChangeHandler call)
        {
            permanentSubscriptions -= call;
        }
        
        public static ObservableData<TType> operator +(ObservableData<TType> lhs, OnChangeHandler rhs)
        {
            lhs.PermanentSubscribe(rhs);
            return lhs;
        }
        
        public static ObservableData<TType> operator *(ObservableData<TType> lhs, OnChangeHandler rhs)
        {
            lhs.SingleSubscribe(rhs);
            return lhs;
        }
        
        public static ObservableData<TType> operator -(ObservableData<TType> lhs, OnChangeHandler rhs)
        {
            lhs.RemovePermanentSubscription(rhs);
            return lhs;
        }
        
        public static ObservableData<TType> operator /(ObservableData<TType> lhs, OnChangeHandler rhs)
        {
            lhs.RemoveSingleSubscription(rhs);
            return lhs;
        }
    }
}