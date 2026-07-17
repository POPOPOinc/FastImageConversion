using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace FastImageConversion
{
    public abstract class EncodingResultHandle : SafeHandle
    {
        protected EncodingResultHandle(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
        {
        }

        public abstract NativeArray<byte> AsNativeArray();
    }
}
