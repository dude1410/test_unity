using System;
using BestHTTP.PlatformSupport.IL2CPP;

namespace ArchCore.Buffer.Pool
{
    [Il2CppEagerStaticClassConstruction]
    internal struct BufferDesc
    {
        public static readonly BufferDesc Empty = new BufferDesc(null);

        /// <summary>
        /// The actual reference to the stored byte array.
        /// </summary>
        public readonly byte[] buffer;

        /// <summary>
        /// When the buffer is put back to the pool. Based on this value the pool will calculate the age of the buffer.
        /// </summary>
        public DateTime released;

#if UNITY_EDITOR
        public readonly string stackTrace;
#endif

        public BufferDesc(byte[] buff)
        {
            this.buffer = buff;
            this.released = DateTime.UtcNow;
#if UNITY_EDITOR
            this.stackTrace = BufferPool.EnableDebugStackTraceCollection ? Environment.StackTrace : string.Empty;
#endif
        }

        public override string ToString()
        {
#if UNITY_EDITOR
            if (BufferPool.EnableDebugStackTraceCollection)
                return
                    $"[BufferDesc Size: {this.buffer.Length}, Released: {this.released}, StackTrace: {this.stackTrace}]";
            else
                return $"[BufferDesc Size: {this.buffer.Length}, Released: {this.released}]";
#else
            return string.Format("[BufferDesc Size: {0}, Released: {1}]", this.buffer.Length, this.released);
#endif
        }
    }
}