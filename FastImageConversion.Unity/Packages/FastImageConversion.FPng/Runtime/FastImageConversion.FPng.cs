using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public static partial class FPng
    {
        static FPng()
        {
            NativeMethods.fpng_init();
        }

        public static unsafe FPngEncodingResultHandle Encode(NativeArray<byte> source, uint width, uint height)
        {
            var sourcePtr = (byte*)source.GetUnsafePtr();

            void* outContext = null;
            byte* outData = null;
            nuint outSize = 0;

            var result = NativeMethods.fpng_encode_image_to_memory(
                sourcePtr,
                width,
                height,
                4, // r,g,b,a
                &outData,
                &outSize,
                &outContext,
                0);
            if (!result)
            {
                // 失敗時のバッファはネイティブ側で解放済み (outContext は null)
                throw new FPngEncodingException($"Failed to encode with fpng");
            }
            return new FPngEncodingResultHandle(outContext, outData, (int)outSize);
        }

        /// <summary>
        /// fpngが出力したPNGを RGBA8 にデコードする。
        /// fpng以外が出力したPNGの場合は <see cref="FPngDecodingException"/> (Status = NotFPng) を投げるので、
        /// 汎用PNGデコーダー (FastImageConversion.Png) へフォールバックすること。
        /// </summary>
        public static unsafe FPngDecodingResultHandle Decode(ReadOnlySpan<byte> source)
        {
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            {
                void* outContext = null;
                byte* outData = null;
                nuint outSize = 0;
                uint width = 0, height = 0, channelsInFile = 0;

                var status = (FPngDecodeStatus)NativeMethods.fpng_decode_memory(
                    sourcePtr,
                    (uint)source.Length,
                    &outData,
                    &outSize,
                    &width,
                    &height,
                    &channelsInFile,
                    4, // RGBA8 として受け取る
                    &outContext);

                if (status != FPngDecodeStatus.Success)
                {
                    // 失敗時のバッファはネイティブ側で解放済み (outContext は null)
                    throw new FPngDecodingException(status);
                }
                return new FPngDecodingResultHandle(outContext, outData, (int)outSize, width, height, channelsInFile);
            }
        }
    }
}
