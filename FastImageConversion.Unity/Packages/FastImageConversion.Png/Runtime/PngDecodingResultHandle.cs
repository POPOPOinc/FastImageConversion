using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public class PngDecodingResultHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        public uint Width => _source.width;
        public uint Height => _source.height;

        private PngDecodingResult _source;

        internal unsafe PngDecodingResultHandle(PngDecodingResult decodingResult) :
            base((IntPtr)decodingResult.output.ptr, true)
        {
            _source = decodingResult;
        }

        /// <summary>
        /// RGBA8 (GraphicsFormat.R8G8B8A8_UNorm 相当) のデコード結果
        /// </summary>
        public unsafe NativeArray<byte> AsNativeArray()
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(nameof(PngDecodingResultHandle));
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
            if (_source.output.ptr == null) return false;
            NativeMethods.fic_png_decode_dispose(_source);
            _source = default;
            return true;
        }
    }
}
