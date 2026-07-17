using System;
using System.Runtime.InteropServices;

namespace FastImageConversion
{
    internal enum WebpCspMode : uint
    {
        Rgb = 0,
        Rgba = 1,
        Bgr = 2,
        Bgra = 3,
        Argb = 4,
        Rgba4444 = 5,
        Rgb565 = 6,
        RgbaPremultiplied = 7,
        BgraPremultiplied = 8,
        ArgbPremultiplied = 9,
        Rgba4444Premultiplied = 10,
        Yuv = 11,
        Yuva = 12,
        Last = 13,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPRGBABuffer
    {
        public IntPtr RGBA;
        public int Stride;
        public UIntPtr Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPYUVABuffer
    {
        public IntPtr Y;
        public IntPtr U;
        public IntPtr V;
        public IntPtr A;
        public int YStride;
        public int UStride;
        public int VStride;
        public int AStride;
        public UIntPtr YSize;
        public UIntPtr USize;
        public UIntPtr VSize;
        public UIntPtr ASize;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct WebPDecBufferUnion
    {
        [FieldOffset(0)]
        public WebPRGBABuffer RGBA;

        [FieldOffset(0)]
        public WebPYUVABuffer YUVA;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPDecBuffer
    {
        public WebpCspMode Colorspace;
        public int Width;
        public int Height;
        public int IsExternalMemory;
        public WebPDecBufferUnion U;
        public unsafe fixed uint Pad[4];
        public IntPtr PrivateMemory;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPBitstreamFeatures
    {
        public int Width;
        public int Height;
        public int HasAlpha;
        public int HasAnimation;
        public int Format;
        public unsafe fixed uint Pad[5];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecoderOptions
    {
        public int BypassFiltering;
        public int NoFancyUpsampling;
        public int UseCropping;
        public int CropLeft;
        public int CropTop;
        public int CropWidth;
        public int CropHeight;
        public int UseScaling;
        public int ScaledWidth;
        public int ScaledHeight;
        public int UseThreads;
        public int DitheringStrength;
        public int Flip;
        public int AlphaDitheringStrength;
        public unsafe fixed uint Pad[5];
    }
}
