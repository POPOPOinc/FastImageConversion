using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public class FPngEncodingResultHandle : EncodingResultHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;
        
        NativeArray<byte> _nativeArray;

        internal unsafe FPngEncodingResultHandle(
            void* contextPtr,
            byte* dataPtr,
            int dataLength) : base((IntPtr)contextPtr, true)
        {
            _nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                dataPtr,
                dataLength,
                Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _nativeArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif            
        }
        
        public override NativeArray<byte> AsNativeArray()
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(nameof(NativeArrayUnsafeUtility));
            } 
            return _nativeArray;
        }

        protected override unsafe bool ReleaseHandle()
        {
            if (IsInvalid) return false;
            NativeMethods.fpng_free((void*)DangerousGetHandle());
            return true;
        }
    }
}