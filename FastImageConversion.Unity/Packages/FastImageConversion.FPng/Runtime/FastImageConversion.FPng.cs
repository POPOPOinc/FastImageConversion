using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

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
                try
                {
                    throw new FPngEncodingException($"Failed to encode with fpng");
                }
                finally
                {
                    NativeMethods.fpng_free(&outContext);
                }
            }
            return new FPngEncodingResultHandle(outContext, outData, (int)outSize);
        }
    }
}
