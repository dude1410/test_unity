using System;
using UnityEngine;

namespace ArchCore.Pooling
{
    public abstract class PoolGameObject : MonoBehaviour, IPoolObject
    {
        public event Action<IPoolObject> PoolDestroy;
        public event Func<IPoolObject> PoolInstantiate;
        public event Action<PoolGameObject> OnDespawn;

        private bool isSpawned = true;
        private bool destroyCalled;

        void IPoolObject.OnCreated()
        {
            OnCreated();
        }

        void IPoolObject.OnDestroyed()
        {
            destroyCalled = true;
            OnDestroyed();
        }

        void IPoolObject.OnSpawned()
        {
            isSpawned = true;
            OnSpawned();
        }

        void IPoolObject.OnDespawned()
        {
            isSpawned = false;
            OnBeforeDespawned();
            OnDespawn?.Invoke(this);
            OnDespawned();
        }

        protected virtual void OnCreated()
        {
        }

        protected virtual void OnDestroyed()
        {
        }

        protected virtual void OnSpawned()
        {
        }

        protected virtual void OnDespawned()
        {
        }

        protected virtual void OnBeforeDespawned()
        {
        }

        public void Destroy()
        {
            if (isSpawned)
                PoolDestroy?.Invoke(this);
        }

        public PoolGameObject GetPooledCopy()
        {
            return (PoolGameObject) PoolInstantiate?.Invoke();
        }


        protected void OnDestroy()
        {
            if (!destroyCalled)
            {
                OnBeforeDespawned();
                OnDespawned();
                OnDestroyed();
            }
            
            isSpawned = false;
            PoolDestroy = null;
            PoolInstantiate = null;
            OnDespawn = null;
        }
    }
}