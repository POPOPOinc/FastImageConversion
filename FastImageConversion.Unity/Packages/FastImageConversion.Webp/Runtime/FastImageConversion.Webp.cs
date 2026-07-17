using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public static partial class Webp
    {
        public static WebPConfig GetDefaultConfig()
        {
            var defaultConfig = CreateConfig(WebPPreset.Picture);
            defaultConfig.Method = 0;
            defaultConfig.Pass = 1;
            defaultConfig.Segments = 1;
            return defaultConfig;
        }
        
        public static WebPConfig CreateConfig(WebPPreset preset, float qualityFactor = 50f)
        {
            return NativeMethods.fic_webp_new_config(preset, qualityFactor);
        }

        public static unsafe WebpMeta Info(ReadOnlySpan<byte> source)
        {
            fixed (byte* ptr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_info(ptr, source.Length);
                if (result.error_code != WebpDecodingErrorCode.None)
                {
                    throw new WebpDecodingException(result.error_code);
                }
                return new WebpMeta(result.meta);
            }
        }
        
        public static WebPDecoderOptions CreateDecoderOptions() => 
            NativeMethods.fic_webp_new_decoder_options();

        public static WebpDecodingResultHandle Decode(ReadOnlySpan<byte> source)
        {
            return Decode(source, CreateDecoderOptions());
        }

        public static unsafe WebpDecodingResultHandle Decode(
            ReadOnlySpan<byte> source, 
            WebPDecoderOptions options)
        {
            fixed (byte* ptr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_decode(ptr, source.Length, options);
                if (result.error_code != WebpDecodingErrorCode.None)
                {
                    throw new WebpDecodingException(result.error_code);
                }
                return new WebpDecodingResultHandle(result);
            }
        }
        public static unsafe WebpEncodingResultHandle Encode(
            ReadOnlySpan<byte> source, 
            int width, 
            int height,
            WebPConfig? config = null) 
        {
            if (config == null)
            {
                config = GetDefaultConfig();
            }

            fixed (byte* sourcePtr = &source.GetPinnableReference())
            {
                var result = NativeMethods.fic_webp_encode(
                    sourcePtr, 
                    source.Length, 
                    width, 
                    height,
                    config.Value);
            
                if (!result.success)
                {
                    try
                    {
                        throw new WebpEncodingException(result.error_code);
                    }
                    finally
                    {
                        NativeMethods.fic_webp_dispose(result);
                    }
                }
                return new WebpEncodingResultHandle(result);
            }
        }
    }
}