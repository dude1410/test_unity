using System;

namespace ArchCore.Buffer.Pool
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public struct PooledBuffer : IDisposable
    {
        public byte[] Data;
        public int Length;

        public void Dispose()
        {
            if (this.Data != null)
                BufferPool.Release(this.Data);
            this.Data = null;
        }
    }
}