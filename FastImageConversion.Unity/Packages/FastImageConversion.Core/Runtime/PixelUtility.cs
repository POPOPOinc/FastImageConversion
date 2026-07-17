using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace FastImageConversion
{
    [BurstCompile]
    internal struct FlipVerticalInplaceJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe byte* Data;
        public int RowBytes;
        public int Height;

        public unsafe void Execute()
        {
            var halfHeight = Height / 2;

            for (var y = 0; y < halfHeight; y++)
            {
                var topPtr = Data + y * RowBytes;
                var bottomPtr = Data + (Height - 1 - y) * RowBytes;
                UnsafeUtility.MemSwap(topPtr, bottomPtr, RowBytes);
            }
        }
    }

    /// <summary>
    /// Helpers for RGBA8 (4 bytes per pixel) pixel buffers.
    /// </summary>
    public static class PixelUtility
    {
        /// <summary>
        /// Flips RGBA8 pixel rows vertically in place.
        /// Use this to convert between Unity's bottom-left origin and the top-left origin
        /// expected by the encoders.
        /// </summary>
        public static void FlipVertically(NativeArray<byte> pixels, int width, int height)
        {
            CheckSize(pixels.Length, width, height);
            unsafe
            {
                new FlipVerticalInplaceJob
                {
                    Data = (byte*)pixels.GetUnsafePtr(),
                    RowBytes = width * 4,
                    Height = height
                }.Run();
            }
        }

        /// <summary>
        /// Copies RGBA8 pixels into <paramref name="destination"/> with the row order reversed.
        /// </summary>
        public static unsafe void CopyFlippedVertically(
            NativeArray<byte> source,
            NativeArray<byte> destination,
            int width,
            int height)
        {
            CheckSize(source.Length, width, height);
            CheckSize(destination.Length, width, height);

            var rowBytes = width * 4;
            var srcPtr = (byte*)source.GetUnsafeReadOnlyPtr();
            var dstPtr = (byte*)destination.GetUnsafePtr();
            for (var y = 0; y < height; y++)
            {
                UnsafeUtility.MemCpy(
                    dstPtr + (height - 1 - y) * rowBytes,
                    srcPtr + y * rowBytes,
                    rowBytes);
            }
        }

        /// <summary>
        /// Reads RGBA8 pixels with a top-left origin from a readable <see cref="Texture2D"/>,
        /// in the layout expected by the encoders.
        /// The caller owns the returned array. Must be called from the Unity main thread.
        /// </summary>
        public static NativeArray<byte> GetPixelsTopLeft(Texture2D texture, Allocator allocator)
        {
            var width = texture.width;
            var height = texture.height;
            var result = new NativeArray<byte>(width * height * 4, allocator, NativeArrayOptions.UninitializedMemory);

            if (texture.format == TextureFormat.RGBA32)
            {
                var raw = texture.GetPixelData<byte>(0);
                CopyFlippedVertically(raw, result, width, height);
            }
            else
            {
                // Fall back to GetPixels32, which converts from any readable format (bottom-left origin, RGBA bytes)
                var colors = texture.GetPixels32();
                unsafe
                {
                    var rowBytes = width * 4;
                    var dstPtr = (byte*)result.GetUnsafePtr();
                    fixed (Color32* srcPtr = colors)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            UnsafeUtility.MemCpy(
                                dstPtr + (height - 1 - y) * rowBytes,
                                (byte*)srcPtr + y * rowBytes,
                                rowBytes);
                        }
                    }
                }
            }
            return result;
        }

        static void CheckSize(int actualLength, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException($"Invalid image size: {width}x{height}");
            }
            if (actualLength < width * height * 4)
            {
                throw new ArgumentException(
                    $"Pixel buffer is too small: {actualLength} bytes for {width}x{height} RGBA8 (requires {width * height * 4} bytes)");
            }
        }
    }
}
