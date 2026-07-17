using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public class FPngDecodingResultHandle : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        public uint Width { get; }
        public uint Height { get; }
        /// <summary>
        /// 元ファイルのチャンネル数 (3 = RGB, 4 = RGBA)。デコード結果は常にRGBA8
        /// </summary>
        public uint ChannelsInFile { get; }

        NativeArray<byte> _nativeArray;

        internal unsafe FPngDecodingResultHandle(
            void* contextPtr,
            byte* dataPtr,
            int dataLength,
            uint width,
            uint height,
            uint channelsInFile) : base((IntPtr)contextPtr, true)
        {
            Width = width;
            Height = height;
            ChannelsInFile = channelsInFile;
            _nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                dataPtr,
                dataLength,
                Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _nativeArray, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
        }

        /// <summary>
        /// RGBA8 (GraphicsFormat.R8G8B8A8_UNorm 相当) のデコード結果
        /// </summary>
        public NativeArray<byte> AsNativeArray()
        {
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException(nameof(FPngDecodingResultHandle));
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
