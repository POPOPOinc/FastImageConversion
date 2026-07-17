using System;
using Unity.Collections;
using UnityEngine;

namespace FastImageConversion
{
    /// <summary>
    /// PNG encoder/decoder backed by fpng (very fast, SSE-optimized).
    /// The decoder only reads PNGs produced by fpng itself — use <see cref="TryDecode"/> and
    /// fall back to a general-purpose PNG decoder when it returns false.
    /// All APIs except the Texture2D helpers are callable from any thread.
    /// </summary>
    public static partial class FPng
    {
        static FPng()
        {
            NativeMethods.fpng_init();
        }

        /// <summary>
        /// Encodes RGBA8 pixels (top-left origin) into PNG.
        /// </summary>
        public static unsafe FPngEncodingResultHandle Encode(ReadOnlySpan<byte> pixels, int width, int height)
        {
            CheckEncodeArgs(pixels.Length, width, height);
            fixed (byte* pixelsPtr = &pixels.GetPinnableReference())
            {
                void* outContext = null;
                byte* outData = null;
                nuint outSize = 0;

                var result = NativeMethods.fpng_encode_image_to_memory(
                    pixelsPtr,
                    (uint)width,
                    (uint)height,
                    4, // r,g,b,a
                    &outData,
                    &outSize,
                    &outContext,
                    0);
                if (!result)
                {
                    // On failure the native side has already freed the buffer (outContext is null)
                    throw new FPngEncodingException("Failed to encode with fpng");
                }
                return new FPngEncodingResultHandle(outContext, outData, (int)outSize);
            }
        }

        /// <inheritdoc cref="Encode(ReadOnlySpan{byte}, int, int)"/>
        public static FPngEncodingResultHandle Encode(NativeArray<byte> pixels, int width, int height)
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
        /// Decodes a PNG produced by fpng into RGBA8 pixels (top-left origin).
        /// Returns false when the input is a valid PNG that was not produced by fpng
        /// (<see cref="FPngDecodeStatus.NotFPng"/>) — fall back to a general-purpose decoder
        /// such as <c>FastImageConversion.Png</c> in that case.
        /// Throws <see cref="FPngDecodingException"/> for any other failure.
        /// </summary>
        public static unsafe bool TryDecode(ReadOnlySpan<byte> source, out FPngDecodingResultHandle decoded)
        {
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            {
                void* outContext = null;
                byte* outData = null;
                nuint outSize = 0;
                uint width = 0, height = 0, sourceChannels = 0;

                var status = (FPngDecodeStatus)NativeMethods.fpng_decode_memory(
                    sourcePtr,
                    (uint)source.Length,
                    &outData,
                    &outSize,
                    &width,
                    &height,
                    &sourceChannels,
                    4, // decode as RGBA8
                    &outContext);

                switch (status)
                {
                    case FPngDecodeStatus.Success:
                        decoded = new FPngDecodingResultHandle(
                            outContext, outData, (int)outSize, (int)width, (int)height, (int)sourceChannels);
                        return true;
                    case FPngDecodeStatus.NotFPng:
                        // The buffer has already been freed by the native side (outContext is null)
                        decoded = null;
                        return false;
                    default:
                        throw new FPngDecodingException(status);
                }
            }
        }

        /// <summary>
        /// Decodes a PNG produced by fpng into RGBA8 pixels (top-left origin).
        /// Throws <see cref="FPngDecodingException"/> (Status = <see cref="FPngDecodeStatus.NotFPng"/>)
        /// when the input was not produced by fpng. Prefer <see cref="TryDecode"/> when a fallback
        /// to a general-purpose decoder is expected.
        /// </summary>
        public static FPngDecodingResultHandle Decode(ReadOnlySpan<byte> source)
        {
            if (!TryDecode(source, out var decoded))
            {
                throw new FPngDecodingException(FPngDecodeStatus.NotFPng);
            }
            return decoded;
        }

        static void CheckEncodeArgs(int pixelsLength, int width, int height)
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
}
