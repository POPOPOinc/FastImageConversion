using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FastImageConversion
{
    public static partial class Png
    {
        public static unsafe PngEncodingResultHandle Encode(NativeArray<byte> source, uint width, uint height) 
        {
            var sourcePtr = (byte*)source.GetUnsafePtr();
            var result = NativeMethods.fic_png_encode(sourcePtr, source.Length, width, height);
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
}
