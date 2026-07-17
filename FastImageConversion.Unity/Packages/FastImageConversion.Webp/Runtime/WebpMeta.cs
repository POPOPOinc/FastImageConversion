namespace FastImageConversion
{
    public enum WebpFormat
    {
        Undefined,
        Lossy,
        Lossless,
    }
    
    public struct WebpMeta
    {
        public int Width;
        public int Height;
        public bool HasAlpha;
        public bool HasAnimation;
        public WebpFormat Format;

        internal WebpMeta(WebPBitstreamFeatures features)
        {
            Width = features.Width;
            Height = features.Height;
            HasAlpha = features.HasAlpha != 0;
            HasAnimation = features.HasAnimation != 0;
            Format = features.Format switch
            {
                1 => WebpFormat.Lossy,
                2 => WebpFormat.Lossless,
                _ => WebpFormat.Undefined
            };
        }
    }
}
