using System;
using UnityEngine;

namespace ArchCore.Pooling
{
    public class PoolData
    {
        public int minSize, maxSize;
        public readonly MonoBehaviour monoSample;
        public readonly string name;
        
        public PoolData(int minSize, int maxSize, MonoBehaviour monoSample, string name)
        {
            this.monoSample = monoSample;
            this.name = name;
            this.minSize = minSize;
            this.maxSize = maxSize;
        }
    }
}