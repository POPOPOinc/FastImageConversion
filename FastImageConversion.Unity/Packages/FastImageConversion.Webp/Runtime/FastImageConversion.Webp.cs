using System;
using Unity.Collections;
using UnityEngine;

namespace FastImageConversion
{
    /// <summary>
    /// WebP encoder/decoder backed by libwebp.
    /// All APIs except the Texture2D helpers are callable from any thread.
    /// </summary>
    public static partial class WebP
    {
        /// <summary>
        /// Creates an encoder configuration from a libwebp preset.
        /// </summary>
        public static WebPConfig CreateConfig(WebPPreset preset, float qualityFactor = 50f)
        {
            return NativeMethods.fic_webp_new_config(preset, qualityFactor);
        }

        /// <summary>
        /// Creates a lossy configuration tuned for encoding speed
        /// (method 0, single pass, single segment). This is the default used by
        /// <see cref="Encode(ReadOnlySpan{byte}, int, int, WebPConfig?)"/>.
        /// </summary>
        public static WebPConfig CreateFastConfig(float qualityFactor = 50f)
        {
            var config = CreateConfig(WebPPreset.Picture, qualityFactor);
            config.Method = 0;
            config.Pass = 1;
            config.Segments = 1;
            return config;
        }

        /// <summary>
        /// Creates a lossless configuration. The decoded output is bit-exact with the input.
        /// </summary>
        /// <param name="effort">Compression effort between 0 (fastest) and 100 (smallest).</param>
        public static WebPConfig CreateLosslessConfig(float effort = 0f)
        {
            var config = CreateConfig(WebPPreset.Default, effort);
            config.Lossless = 1;
            config.Exact = 1;
            return config;
        }

        public static WebPDecoderOptions CreateDecoderOptions() =>
            NativeMethods.fic_webp_new_decoder_options();

        /// <summary>
        /// Reads the WebP header and returns image metadata without decoding pixels.
        /// </summary>
        public static unsafe WebPMeta Info(ReadOnlySpan<byte> source)
        {
            fixed (byte* ptr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_info(ptr, source.Length);
                if (result.error_code != WebpDecodingErrorCode.None)
                {
                    throw new WebPDecodingException((WebPDecodingError)result.error_code);
                }
                return new WebPMeta(result.meta);
            }
        }

        /// <summary>
        /// Encodes RGBA8 pixels (top-left origin) into WebP.
        /// When <paramref name="config"/> is omitted, <see cref="CreateFastConfig"/> is used.
        /// </summary>
        public static unsafe WebPEncodingResultHandle Encode(
            ReadOnlySpan<byte> pixels,
            int width,
            int height,
            WebPConfig? config = null)
        {
            CheckEncodeArgs(pixels.Length, width, height);
            config ??= CreateFastConfig();

            fixed (byte* pixelsPtr = &pixels.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_encode(
                    pixelsPtr,
                    pixels.Length,
                    width,
                    height,
                    config.Value);

                if (!result.success)
                {
                    try
                    {
                        throw new WebPEncodingException((WebPEncodingError)result.error_code);
                    }
                    finally
                    {
                        NativeMethods.fic_webp_dispose(result);
                    }
                }
                return new WebPEncodingResultHandle(result);
            }
        }

        /// <inheritdoc cref="Encode(ReadOnlySpan{byte}, int, int, WebPConfig?)"/>
        public static WebPEncodingResultHandle Encode(
            NativeArray<byte> pixels,
            int width,
            int height,
            WebPConfig? config = null)
        {
            return Encode(pixels.AsReadOnlySpan(), width, height, config);
        }

        /// <summary>
        /// Encodes a readable <see cref="Texture2D"/> into WebP.
        /// Handles the vertical flip from Unity's bottom-left origin.
        /// Must be called from the Unity main thread.
        /// </summary>
        public static byte[] EncodeTexture(Texture2D texture, WebPConfig? config = null)
        {
            using var pixels = PixelUtility.GetPixelsTopLeft(texture, Allocator.Temp);
            using var encoded = Encode(pixels, texture.width, texture.height, config);
            return encoded.ToArray();
        }

        /// <summary>
        /// Decodes a WebP into RGBA8 pixels (top-left origin).
        /// </summary>
        public static WebPDecodingResultHandle Decode(ReadOnlySpan<byte> source)
        {
            return Decode(source, CreateDecoderOptions());
        }

        /// <inheritdoc cref="Decode(ReadOnlySpan{byte})"/>
        public static unsafe WebPDecodingResultHandle Decode(
            ReadOnlySpan<byte> source,
            WebPDecoderOptions options)
        {
            fixed (byte* ptr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_decode(ptr, source.Length, options);
                if (result.error_code != WebpDecodingErrorCode.None)
                {
                    throw new WebPDecodingException((WebPDecodingError)result.error_code);
                }
                return new WebPDecodingResultHandle(result);
            }
        }

        /// <summary>
        /// Decodes a WebP into a new <see cref="Texture2D"/>.
        /// Handles the vertical flip to Unity's bottom-left origin.
        /// Must be called from the Unity main thread.
        /// </summary>
        public static Texture2D DecodeToTexture(ReadOnlySpan<byte> source, bool linear = false)
        {
            using var decoded = Decode(source);
            return decoded.ToTexture2D(linear);
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
