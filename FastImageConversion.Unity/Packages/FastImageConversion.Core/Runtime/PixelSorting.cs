using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace FastImageConversion
{
    [BurstCompile]
    internal struct FlipVerticalInplaceJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public unsafe byte* Data;
        public int Size;
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

    public static class PixelSorting
    {
        public static void FlipVerticalInplace(NativeArray<byte> src, int width, int height)
        {
            unsafe
            {
                new FlipVerticalInplaceJob
                {
                    Data = (byte*)src.GetUnsafePtr(),
                    RowBytes = width * 4,
                    Height = height
                }.Run();
            }
        }
    }
}