using System;
using UnityEngine;

namespace ArchCore.Pooling
{
    [Serializable]
    public class PoolContainerData
    {
        public int minSize, maxSize;
        public MonoBehaviour sample;
        public string name;
        public bool useObjectName = true;

        public PoolContainerData(int minSize, int maxSize)
        {
            this.minSize = minSize;
            this.maxSize = maxSize;
        }
    }
}