using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public class WebpDecodingResultHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        public WebpMeta Meta => new(_source.meta);

        private WebpDecodingResult _source;

        internal unsafe WebpDecodingResultHandle(WebpDecodingResult decodingResult) :
            base(decodingResult.output.U.RGBA.RGBA, true)
        {
            _source = decodingResult;
        }

        public unsafe NativeArray<byte> AsNativeArray()
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(nameof(WebpDecodingResultHandle));
            }

            var rgba = _source.output.U.RGBA;
            var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                (void*)rgba.RGBA,
                (int)rgba.Size,
                Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            return nativeArray;
        }

        protected override bool ReleaseHandle()
        {
            if (_source.output.U.RGBA.RGBA == IntPtr.Zero) return true;
            NativeMethods.fic_webp_decode_dispose(_source);
            _source = default;
            return true;
        }
    }
}
