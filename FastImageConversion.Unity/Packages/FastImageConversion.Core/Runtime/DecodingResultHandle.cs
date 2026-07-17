using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace FastImageConversion
{
    /// <summary>
    /// Owns the native memory of a decoded image.
    /// The pixel data is RGBA8 (<c>GraphicsFormat.R8G8B8A8_UNorm</c> compatible) with a top-left origin.
    /// </summary>
    public abstract class DecodingResultHandle : NativeMemoryHandle
    {
        public abstract int Width { get; }
        public abstract int Height { get; }

        protected DecodingResultHandle(IntPtr handleValue, bool ownsHandle) : base(handleValue, ownsHandle)
        {
        }

        /// <summary>
        /// Zero-copy view of the decoded RGBA8 pixels (top-left origin).
        /// Invalidated when this handle is disposed.
        /// </summary>
        public abstract NativeArray<byte> AsNativeArray();

        /// <summary>
        /// Zero-copy span over the decoded RGBA8 pixels. Do not use it after this handle is disposed.
        /// </summary>
        public unsafe ReadOnlySpan<byte> AsSpan()
        {
            var array = AsNativeArray();
            return new ReadOnlySpan<byte>(array.GetUnsafeReadOnlyPtr(), array.Length);
        }

        /// <summary>
        /// Copies the decoded RGBA8 pixels into a new managed array.
        /// </summary>
        public byte[] ToArray() => AsNativeArray().ToArray();

        /// <summary>
        /// Creates a <see cref="Texture2D"/> from the decoded pixels.
        /// Handles the vertical flip between the image's top-left origin and Unity's bottom-left origin.
        /// Must be called from the Unity main thread.
        /// </summary>
        public Texture2D ToTexture2D(bool linear = false)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, mipChain: false, linear);
            var dst = texture.GetPixelData<byte>(0);
            PixelUtility.CopyFlippedVertically(AsNativeArray(), dst, Width, Height);
            texture.Apply(updateMipmaps: false);
            return texture;
        }
    }
}
