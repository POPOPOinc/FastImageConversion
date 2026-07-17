using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class FPngEncodingResultHandle : EncodingResultHandle
    {
        unsafe byte* _dataPtr;
        readonly int _dataLength;

        internal unsafe FPngEncodingResultHandle(
            void* contextPtr,
            byte* dataPtr,
            int dataLength) : base((IntPtr)contextPtr, true)
        {
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
