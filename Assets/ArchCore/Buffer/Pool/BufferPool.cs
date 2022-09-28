using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BestHTTP.PlatformSupport.IL2CPP;
using UnityEngine;
using Object = System.Object;

#if NET_STANDARD_2_0 || NETFX_CORE
using System.Runtime.CompilerServices;
#endif

namespace ArchCore.Buffer.Pool
{
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    [Il2CppEagerStaticClassConstruction]
    public static class BufferPool
    {
        public static readonly byte[] NoData = new byte[0];

        /// <summary>
        /// Setting this property to false the pooling mechanism can be disabled.
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;

                // When set to non-enabled remove all stored entries
                if (!_isEnabled)
                    Clear();
            }
        }

        private static volatile bool _isEnabled = true;

        /// <summary>
        /// Buffer entries that released back to the pool and older than this value are moved when next maintenance is triggered.
        /// </summary>
        public static TimeSpan RemoveOlderThan = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How often pool maintenance must run.
        /// </summary>
        public static TimeSpan RunMaintenanceEvery = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Minimum buffer size that the plugin will allocate when the requested size is smaller than this value, and canBeLarger is set to true.
        /// </summary>
        public static long MinBufferSize = 256;

        /// <summary>
        /// Maximum size of a buffer that the plugin will store.
        /// </summary>
        public static long MaxBufferSize = long.MaxValue;

        /// <summary>
        /// Maximum accumulated size of the stored buffers.
        /// </summary>
        public static long MaxPoolSize = 10 * 1024 * 1024;

        /// <summary>
        /// Whether to remove empty buffer stores from the free list.
        /// </summary>
        public static bool RemoveEmptyLists = false;

        /// <summary>
        /// If it set to true and a byte[] is released more than once it will log out an error.
        /// </summary>
        public static bool IsDoubleReleaseCheckEnabled = false;

#if UNITY_EDITOR
        /// <summary>
        /// When set to true, the plugin collects Get and Release stack trace informations.
        /// </summary>
        public static bool EnableDebugStackTraceCollection = false;
#endif

        // It must be sorted by buffer size!
        private static readonly List<BufferStore> FreeBuffers = new List<BufferStore>();
        private static DateTime lastMaintenance = DateTime.MinValue;

        // Statistics
        private static long PoolSize = 0;
        private static long GetBuffers = 0;
        private static long ReleaseBuffers = 0;
        private static readonly StringBuilder statiscticsBuilder = new StringBuilder();

        private static readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

#if UNITY_EDITOR
        private static readonly Dictionary<string, int> getStackStats = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> releaseStackStats = new Dictionary<string, int>();
#endif

        static BufferPool()
        {
#if UNITY_EDITOR
            IsDoubleReleaseCheckEnabled = true;
#else
            IsDoubleReleaseCheckEnabled = false;
#endif
        }


        /// <summary>
        /// Get byte[] from the pool. If canBeLarge is true, the returned buffer might be larger than the requested size.
        /// </summary>
        public static byte[] Get(long size, bool canBeLarger)
        {
            if (!_isEnabled)
                return new byte[size];

            // Return a fix reference for 0 length requests. Any resize call (even Array.Resize) creates a new reference
            //  so we are safe to expose it to multiple callers.
            if (size == 0)
                return NoData;

#if UNITY_EDITOR
            if (EnableDebugStackTraceCollection)
            {
                lock (getStackStats)
                {
                    string stack = ProcessStackTrace(Environment.StackTrace);
                    int value;
                    if (!getStackStats.TryGetValue(stack, out value))
                        getStackStats.Add(stack, 1);
                    else
                        getStackStats[stack] = ++value;
                }
            }
#endif

            if (canBeLarger)
            {
                if (size < MinBufferSize)
                    size = MinBufferSize;
                else if (!IsPowerOfTwo(size))
                    size = NextPowerOf2(size);
            }

            if (FreeBuffers.Count == 0)
                return new byte[size];

            BufferDesc bufferDesc = FindFreeBuffer(size, canBeLarger);

            if (bufferDesc.buffer == null)
                return new byte[size];
            else
                Interlocked.Increment(ref GetBuffers);

            Interlocked.Add(ref PoolSize, -bufferDesc.buffer.Length);

            return bufferDesc.buffer;
        }

        /// <summary>
        /// Release back a BufferSegment's data to the pool.
        /// </summary>
        /// <param name="segment"></param>
        public static void Release(ArraySegment<byte> segment)
        {
            Release(segment.Array);
        }

        /// <summary>
        /// Release back a byte array to the pool.
        /// </summary>
        public static void Release(byte[] buffer)
        {
            if (!_isEnabled || buffer == null)
                return;

#if UNITY_EDITOR
            if (EnableDebugStackTraceCollection)
            {
                lock (releaseStackStats)
                {
                    string stack = ProcessStackTrace(Environment.StackTrace);
                    int value;
                    if (!releaseStackStats.TryGetValue(stack, out value))
                        releaseStackStats.Add(stack, 1);
                    else
                        releaseStackStats[stack] = ++value;
                }
            }
#endif

            int size = buffer.Length;

            if (size == 0 || size > MaxBufferSize)
                return;

            rwLock.EnterWriteLock();
            try
            {
                if (PoolSize + size > MaxPoolSize)
                    return;
                PoolSize += size;

                ReleaseBuffers++;

                AddFreeBuffer(buffer);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Resize a byte array. It will release the old one to the pool, and the new one is from the pool too.
        /// </summary>
        public static byte[] Resize(ref byte[] buffer, int newSize, bool canBeLarger, bool clear)
        {
            if (!_isEnabled)
            {
                Array.Resize<byte>(ref buffer, newSize);
                return buffer;
            }

            byte[] newBuf = Get(newSize, canBeLarger);
            if (buffer != null)
            {
                if (!clear)
                    Array.Copy(buffer, 0, newBuf, 0, Math.Min(newBuf.Length, buffer.Length));
                Release(buffer);
            }

            if (clear)
                Array.Clear(newBuf, 0, newSize);

            return buffer = newBuf;
        }

        /// <summary>
        /// Get textual statistics about the buffer pool.
        /// </summary>
        public static string GetStatistics(bool showEmptyBuffers = true)
        {
            rwLock.EnterReadLock();
            try
            {
                statiscticsBuilder.Length = 0;
                statiscticsBuilder.AppendFormat("Pooled array reused count: {0:N0}\n", GetBuffers);
                statiscticsBuilder.AppendFormat("Release call count: {0:N0}\n", ReleaseBuffers);
                statiscticsBuilder.AppendFormat("PoolSize: {0:N0}\n", PoolSize);
                statiscticsBuilder.AppendFormat("Buffers: {0}\n", FreeBuffers.Count);

                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    if (showEmptyBuffers || buffers.Count > 0)
                        statiscticsBuilder.AppendFormat("- Size: {0:N0} Count: {1:N0}\n", store.Size, buffers.Count);
                }

#if UNITY_EDITOR
                if (EnableDebugStackTraceCollection)
                {
                    lock (getStackStats)
                    {
                        int sum = 0;
                        foreach (var kvp in getStackStats)
                            sum += kvp.Value;

                        statiscticsBuilder.AppendFormat("Get stacks: {0:N0}\n", sum);

                        foreach (var kvp in getStackStats)
                        {
                            statiscticsBuilder.AppendFormat("- {0:N0}: {1}\n", kvp.Value, kvp.Key);
                        }
                    }

                    lock (releaseStackStats)
                    {
                        int sum = 0;
                        foreach (var kvp in releaseStackStats)
                            sum += kvp.Value;

                        statiscticsBuilder.AppendFormat("Release stacks: {0:N0}\n", sum);

                        foreach (var kvp in releaseStackStats)
                        {
                            statiscticsBuilder.AppendFormat("- {0:N0}: {1}\n", kvp.Value, kvp.Key);
                        }
                    }
                }
#endif

                return statiscticsBuilder.ToString();
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove all stored entries instantly.
        /// </summary>
        public static void Clear()
        {
            rwLock.EnterWriteLock();
            try
            {
                FreeBuffers.Clear();
                PoolSize = 0;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Internal function called by the plugin to remove old, non-used buffers.
        /// </summary>
        internal static void Maintain()
        {
            DateTime now = DateTime.UtcNow;
            if (!_isEnabled || lastMaintenance + RunMaintenanceEvery > now)
                return;
            lastMaintenance = now;

            //if (HTTPManager.Logger.Level == Logger.Loglevels.All)
            //    HTTPManager.Logger.Information("BufferPool", "Before Maintain: " + GetStatistics());

            DateTime olderThan = now - RemoveOlderThan;
            rwLock.EnterWriteLock();
            try
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];
                    List<BufferDesc> buffers = store.buffers;

                    for (int cv = buffers.Count - 1; cv >= 0; cv--)
                    {
                        BufferDesc desc = buffers[cv];

                        if (desc.released < olderThan)
                        {
                            // buffers stores available buffers ascending by age. So, when we find an old enough, we can
                            //  delete all entries in the [0..cv] range.

                            int removeCount = cv + 1;
                            buffers.RemoveRange(0, removeCount);
                            PoolSize -= (int) (removeCount * store.Size);
                            break;
                        }
                    }

                    if (RemoveEmptyLists && buffers.Count == 0)
                        FreeBuffers.RemoveAt(i--);
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            //if (HTTPManager.Logger.Level == Logger.Loglevels.All)
            //    HTTPManager.Logger.Information("BufferPool", "After Maintain: " + GetStatistics());
        }

        #region Private helper functions

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool IsPowerOfTwo(long x)
        {
            return (x & (x - 1)) == 0;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static long NextPowerOf2(long x)
        {
            long pow = 1;
            while (pow <= x)
                pow *= 2;
            return pow;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static BufferDesc FindFreeBuffer(long size, bool canBeLarger)
        {
            rwLock.EnterUpgradeableReadLock();
            try
            {
                for (int i = 0; i < FreeBuffers.Count; ++i)
                {
                    BufferStore store = FreeBuffers[i];

                    if (store.buffers.Count > 0 && (store.Size == size || (canBeLarger && store.Size > size)))
                    {
                        // Getting the last one has two desired effect:
                        //  1.) RemoveAt should be quicker as it don't have to move all the remaining entries
                        //  2.) Old, non-used buffers will age. Getting a buffer and putting it back will not keep buffers fresh.

                        BufferDesc lastFree = store.buffers[store.buffers.Count - 1];

                        rwLock.EnterWriteLock();
                        try
                        {
                            store.buffers.RemoveAt(store.buffers.Count - 1);
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }

                        return lastFree;
                    }
                }
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }

            return BufferDesc.Empty;
        }

#if NET_STANDARD_2_0 || NETFX_CORE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void AddFreeBuffer(byte[] buffer)
        {
            int bufferLength = buffer.Length;

            for (int i = 0; i < FreeBuffers.Count; ++i)
            {
                BufferStore store = FreeBuffers[i];

                if (store.Size == bufferLength)
                {
                    // We highly assume here that every buffer will be released only once.
                    //  Checking for double-release would mean that we have to do another O(n) operation, where n is the
                    //  count of the store's elements.

                    if (IsDoubleReleaseCheckEnabled)
                        for (int cv = 0; cv < store.buffers.Count; ++cv)
                        {
                            var entry = store.buffers[cv];
                            if (ReferenceEquals(entry.buffer, buffer))
                            {
                                Debug.LogError(
                                    ("BufferPool", $"Buffer ({entry.ToString()}) already added to the pool!"));
                                return;
                            }
                        }

                    store.buffers.Add(new BufferDesc(buffer));
                    return;
                }

                if (store.Size > bufferLength)
                {
                    FreeBuffers.Insert(i, new BufferStore(bufferLength, buffer));
                    return;
                }
            }

            // When we reach this point, there's no same sized or larger BufferStore present, so we have to add a new one
            //  to the end of our list.
            FreeBuffers.Add(new BufferStore(bufferLength, buffer));
        }

#if UNITY_EDITOR
        private static string ProcessStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            var lines = stackTrace.Split('\n');

            StringBuilder sb = new StringBuilder(lines.Length);
            // skip top 4 lines that would show the logger.
            for (int i = 2; i < Math.Min(5, lines.Length); ++i)
                sb.Append(lines[i].Replace("BestHTTP.", ""));

            return sb.ToString();
        }
#endif

        #endregion
    }
}