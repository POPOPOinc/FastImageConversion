using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public class WebpEncodingResultHandle : EncodingResultHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        private WebpEncodingResult _source;

        internal unsafe WebpEncodingResultHandle(WebpEncodingResult encodingResult) :
            base((IntPtr)encodingResult.output.ptr, true)
        {
            _source = encodingResult;
        }

        public override unsafe NativeArray<byte> AsNativeArray()
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(nameof(NativeArrayUnsafeUtility));
            }

            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                _source.output.ptr,
                _source.output.length,
                Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return nativeArray;
        }

        protected override unsafe bool ReleaseHandle()
        {
            if (_source.output.ptr == null) return true;
            NativeMethods.fic_webp_dispose(_source);
            _source = default;
            return true;
        }
    }
}
