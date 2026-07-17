using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class FPngDecodingResultHandle : DecodingResultHandle
    {
        public override int Width { get; }
        public override int Height { get; }

        /// <summary>
        /// Number of channels in the source file (3 = RGB, 4 = RGBA).
        /// The decoded pixel data is always RGBA8 regardless of this value.
        /// </summary>
        public int SourceChannels { get; }

        unsafe byte* _dataPtr;
        readonly int _dataLength;

        internal unsafe FPngDecodingResultHandle(
            void* contextPtr,
            byte* dataPtr,
            int dataLength,
            int width,
            int height,
            int sourceChannels) : base((IntPtr)contextPtr, true)
        {
            Width = width;
            Height = height;
            SourceChannels = sourceChannels;
            _dataPtr = dataPtr;
            _dataLength = dataLength;
        }

        public override unsafe NativeArray<byte> AsNativeArray()
        {
            return CreateView(_dataPtr, _dataLength);
        }

        protected override unsafe bool ReleaseNativeMemory()
        {
            NativeMethods.fpng_free((void*)handle);
            _dataPtr = null;
            return true;
        }
    }
}
