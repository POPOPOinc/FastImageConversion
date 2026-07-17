using System;
using Unity.Collections;
using UnityEngine;

namespace FastImageConversion
{
    /// <summary>
    /// PNG encoder/decoder backed by image-rs/png (general purpose).
    /// All APIs except the Texture2D helpers are callable from any thread.
    /// </summary>
    public static partial class Png
    {
        /// <summary>
        /// Encodes RGBA8 pixels (top-left origin) into PNG.
        /// </summary>
        public static unsafe PngEncodingResultHandle Encode(ReadOnlySpan<byte> pixels, int width, int height)
        {
            CheckEncodeArgs(pixels.Length, width, height);
            fixed (byte* pixelsPtr = &pixels.GetPinnableReference())
            {
                var result = NativeMethods.fic_png_encode(pixelsPtr, pixels.Length, (uint)width, (uint)height);
                if (!result.success)
                {
                    try
                    {
                        var message = new string((sbyte*)result.error_message);
                        throw new PngEncodingException(message);
                    }
                    finally
                    {
                        NativeMethods.fic_png_dispose(result);
                    }
                }
                return new PngEncodingResultHandle(result);
            }
        }

        /// <inheritdoc cref="Encode(ReadOnlySpan{byte}, int, int)"/>
        public static PngEncodingResultHandle Encode(NativeArray<byte> pixels, int width, int height)
        {
            return Encode(pixels.AsReadOnlySpan(), width, height);
        }

        /// <summary>
        /// Encodes a readable <see cref="Texture2D"/> into PNG.
        /// Handles the vertical flip from Unity's bottom-left origin.
        /// Must be called from the Unity main thread.
        /// </summary>
        public static byte[] EncodeTexture(Texture2D texture)
        {
            using var pixels = PixelUtility.GetPixelsTopLeft(texture, Allocator.Temp);
            using var encoded = Encode(pixels, texture.width, texture.height);
            return encoded.ToArray();
        }

        /// <summary>
        /// Reads the PNG header and returns the image dimensions without decoding pixels.
        /// </summary>
        public static unsafe PngMeta Info(ReadOnlySpan<byte> source)
        {
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_png_info(sourcePtr, source.Length);
                try
                {
                    if (!result.success)
                    {
                        var message = new string((sbyte*)result.error_message);
                        throw new PngDecodingException(message);
                    }
                    return new PngMeta
                    {
                        Width = (int)result.width,
                        Height = (int)result.height,
                    };
                }
                finally
                {
                    NativeMethods.fic_png_info_dispose(result);
                }
            }
        }

        /// <summary>
        /// Decodes an arbitrary PNG into RGBA8 pixels (top-left origin).
        /// </summary>
        public static unsafe PngDecodingResultHandle Decode(ReadOnlySpan<byte> source)
        {
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_png_decode(sourcePtr, source.Length);
                if (!result.success)
                {
                    try
                    {
                        var message = new string((sbyte*)result.error_message);
                        throw new PngDecodingException(message);
                    }
                    finally
                    {
                        NativeMethods.fic_png_decode_dispose(result);
                    }
                }
                return new PngDecodingResultHandle(result);
            }
        }

        /// <summary>
        /// Decodes a PNG into a new <see cref="Texture2D"/>.
        /// Handles the vertical flip to Unity's bottom-left origin.
        /// Must be called from the Unity main thread.
        /// </summary>
        public static Texture2D DecodeToTexture(ReadOnlySpan<byte> source, bool linear = false)
        {
            using var decoded = Decode(source);
            return decoded.ToTexture2D(linear);
        }

        internal static void CheckEncodeArgs(int pixelsLength, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException($"Invalid image size: {width}x{height}");
            }
            if (pixelsLength < width * height * 4)
            {
                throw new ArgumentException(
                    $"Pixel buffer is too small: {pixelsLength} bytes for {width}x{height} RGBA8 (requires {width * height * 4} bytes)");
            }
        }
    }

    /// <summary>
    /// PNG header information.
    /// </summary>
    public struct PngMeta
    {
        public int Width;
        public int Height;
    }
}
