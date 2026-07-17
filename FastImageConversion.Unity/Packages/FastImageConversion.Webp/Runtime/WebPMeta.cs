namespace FastImageConversion
{
    public enum WebPFormat
    {
        Undefined,
        Lossy,
        Lossless,
    }

    /// <summary>
    /// WebP header information.
    /// </summary>
    public struct WebPMeta
    {
        public int Width;
        public int Height;
        public bool HasAlpha;
        public bool HasAnimation;
        public WebPFormat Format;

        internal WebPMeta(WebPBitstreamFeatures features)
        {
            Width = features.Width;
            Height = features.Height;
            HasAlpha = features.HasAlpha != 0;
            HasAnimation = features.HasAnimation != 0;
            Format = features.Format switch
            {
                1 => WebPFormat.Lossy,
                2 => WebPFormat.Lossless,
                _ => WebPFormat.Undefined
            };
        }
    }
}
