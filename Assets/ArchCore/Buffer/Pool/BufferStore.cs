using System.Collections.Generic;
using BestHTTP.PlatformSupport.IL2CPP;

namespace ArchCore.Buffer.Pool
{
    /// <summary>
    /// Private data struct that contains the size <-> byte arrays mapping. 
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    internal struct BufferStore
    {
        /// <summary>
        /// Size/length of the arrays stored in the buffers.
        /// </summary>
        public readonly long Size;

        /// <summary>
        /// 
        /// </summary>
        public readonly List<BufferDesc> buffers;

        public BufferStore(long size)
        {
            this.Size = size;
            this.buffers = new List<BufferDesc>();
        }

        /// <summary>
        /// Create a new store with its first byte[] to store.
        /// </summary>
        public BufferStore(long size, byte[] buffer)
            : this(size)
        {
            this.buffers.Add(new BufferDesc(buffer));
        }

        public override string ToString()
        {
            return $"[BufferStore Size: {this.Size:N0}, Buffers: {this.buffers.Count}]";
        }
    }
}