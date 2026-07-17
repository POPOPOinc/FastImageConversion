using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    /// <summary>
    /// Base class for handles that own native memory produced by the native plugins.
    /// Dispose the handle (e.g. with <c>using</c>) to free the native memory.
    /// </summary>
    public abstract class NativeMemoryHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle _safety;
        bool _safetyCreated;
#endif

        protected NativeMemoryHandle(IntPtr handleValue, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handleValue);
        }

        /// <summary>
        /// Creates a zero-copy <see cref="NativeArray{T}"/> view over native memory owned by this handle.
        /// When collection checks are enabled, the view is invalidated when this handle is disposed.
        /// </summary>
        protected unsafe NativeArray<byte> CreateView(void* ptr, int length)
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!_safetyCreated)
            {
                _safety = AtomicSafetyHandle.Create();
                _safetyCreated = true;
            }
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, _safety);
#endif
            return array;
        }

        protected sealed override bool ReleaseHandle()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (_safetyCreated)
            {
                AtomicSafetyHandle.Release(_safety);
                _safetyCreated = false;
            }
#endif
            return ReleaseNativeMemory();
        }

        /// <summary>
        /// Frees the native memory owned by this handle.
        /// </summary>
        protected abstract bool ReleaseNativeMemory();
    }
}
