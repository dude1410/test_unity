using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ArchCore.Utils;
using UnityEngine;

namespace ArchCore.Pooling
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class GameObjectPool : BaseObjectPool
    {

        private readonly MonoBehaviour sample;
        private readonly Transform storeLocation;

        private int poolMin;
        private int instantiatedMax = 0;
        private int instantiatedCount = 0;

        public GameObjectPool(int minSize, int maxSize, MonoBehaviour sample, Transform storeLocation) : base(minSize, maxSize)
        {
            this.sample = sample;
            this.storeLocation = storeLocation;

            Fill();
        }
        
        void Check()
        {
            if (pool.Count < poolMin)
            {
                poolMin = pool.Count;
            }
            
            if (instantiatedCount > instantiatedMax)
            {
                instantiatedMax = instantiatedCount;
            }
        }

        
        protected sealed override void Fill()
        {
            for (int j = 0; j < minSize; j++)
            {
                var p = Object.Instantiate(sample, storeLocation, true);
                p.gameObject.SetActive(false);
                var poolObject = p as IPoolObject;
                poolObject.PoolDestroy += Destroy;
                pool.Push(poolObject);
                poolObject.OnCreated();
            }
        }

        public override void Clear()
        {
            while (pool.Count > 0)
            {
                var p = pool.Pop();
                p.OnDespawned();
                p.OnDestroyed();
                Object.Destroy((p as MonoBehaviour).gameObject);
            }
        }


        public override MonoBehaviour Instantiate()
        {
            return Instantiate(Vector3.zero);
        }

        public MonoBehaviour Instantiate(Transform parent)
        {
            return Instantiate(Vector3.zero, parent);
        }
        
        public MonoBehaviour Instantiate(Vector3 position, Transform parent = null)
        {
            MonoBehaviour retObj;
            IPoolObject poolObject;

            if (pool.Count > 0)
            {
                poolObject = pool.Pop();
                retObj = poolObject as MonoBehaviour;
            }
            else
            {
                retObj = Object.Instantiate(sample);
                poolObject = retObj as IPoolObject;
                poolObject.PoolDestroy += Destroy;
                poolObject.OnCreated();
            }

            Transform transform = retObj.transform;
            transform.SetParent(parent);
            transform.position = position;
            retObj.gameObject.SetActive(true);
            
            poolObject.OnSpawned();

            instantiatedCount++;
            Check();
            
            return retObj;

        }

        protected override void Destroy(IPoolObject obj)
        {
            instantiatedCount--;
            var poolObject = obj as MonoBehaviour;
            if (pool.Count < maxSize)
            {
                poolObject.gameObject.SetActive(false);
                var transform = poolObject.transform;
                transform.SetParent(storeLocation);
                transform.position = new Vector3(-100, -100, 0);

                pool.Push(obj);
                obj.OnDespawned();
            }
            else
            {
                obj.OnDespawned();
                obj.OnDestroyed();
                Object.Destroy(poolObject.gameObject);
            }
        }

        
    }
}
