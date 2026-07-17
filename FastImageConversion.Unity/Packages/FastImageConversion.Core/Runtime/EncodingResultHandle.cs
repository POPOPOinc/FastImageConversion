using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    /// <summary>
    /// Owns the native memory of an encoded image.
    /// </summary>
    public abstract class EncodingResultHandle : NativeMemoryHandle
    {
        protected EncodingResultHandle(IntPtr handleValue, bool ownsHandle) : base(handleValue, ownsHandle)
        {
        }

        /// <summary>
        /// Zero-copy view of the encoded bytes. Invalidated when this handle is disposed.
        /// </summary>
        public abstract NativeArray<byte> AsNativeArray();

        /// <summary>
        /// Zero-copy span over the encoded bytes. Do not use it after this handle is disposed.
        /// </summary>
        public unsafe ReadOnlySpan<byte> AsSpan()
        {
            var array = AsNativeArray();
            return new ReadOnlySpan<byte>(array.GetUnsafeReadOnlyPtr(), array.Length);
        }

        /// <summary>
        /// Copies the encoded bytes into a new managed array.
        /// </summary>
        public byte[] ToArray() => AsNativeArray().ToArray();
    }
}
