using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Pooling
{
    [CreateAssetMenu(fileName = "SubPoolContainer", menuName = "Utils/SubPoolContainer")]
    public class SubPoolContainer : ScriptableObject
    {
        [SerializeField]
        protected List<PoolContainerData> list = new List<PoolContainerData>();

        public List<PoolContainerData> GetData() => list;

        private void Awake()
        {
            CheckLists();
        }

        private void CheckLists()
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].sample is IPoolObject) continue;
                if (list[i].sample)
                    Debug.LogError($"{list[i].sample.GetType()} of gameobject {list[i].sample.name} dose not contain IPoolObject interface, in pool {name}");
                
                list.RemoveAt(i);
            }
        }
    }
}