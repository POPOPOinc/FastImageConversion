using System.Runtime.InteropServices;

namespace FastImageConversion
{
    public enum WebPImageHint : uint
    {
        Default = 0,
        Picture = 1,
        Photo = 2,
        Graph = 3,
        Last = 4,
    }

    public enum WebPPreset : uint
    {
        Default = 0,
        Picture = 1,
        Photo = 2,
        Drawing = 3,
        Icon = 4,
        Text = 5,
    }

    /// <summary>
    /// WebP encoding configuration.
    /// See <see href="https://developers.google.com/speed/webp/docs/api">WebP API documentation</see>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WebPConfig
    {
        /// <summary>Lossless encoding (0 = lossy (default), 1 = lossless).</summary>
        public int Lossless;

        /// <summary>
        /// Between 0 and 100. For lossy, 0 gives the smallest size and 100 the largest.
        /// For lossless, this parameter is the amount of effort put into the compression:
        /// 0 is the fastest but gives larger files compared to the slowest, but best, 100.
        /// </summary>
        public float Quality;

        /// <summary>Quality/speed trade-off (0 = fast, 6 = slower-better).</summary>
        public int Method;

        /// <summary>Hint for image type (lossless only for now).</summary>
        public WebPImageHint ImageHint;

        /// <summary>
        /// If non-zero, set the desired target size in bytes.
        /// Takes precedence over the <see cref="Quality"/> parameter.
        /// </summary>
        public int TargetSize;

        /// <summary>
        /// If non-zero, specifies the minimal distortion to try to achieve.
        /// Takes precedence over <see cref="TargetSize"/>.
        /// </summary>
        public float TargetPsnr;

        /// <summary>Maximum number of segments to use, in [1..4].</summary>
        public int Segments;

        /// <summary>Spatial Noise Shaping. 0 = off, 100 = maximum.</summary>
        public int SnsStrength;

        /// <summary>Range: [0 = off .. 100 = strongest].</summary>
        public int FilterStrength;

        /// <summary>Range: [0 = off .. 7 = least sharp].</summary>
        public int FilterSharpness;

        /// <summary>
        /// Filtering type: 0 = simple, 1 = strong.
        /// Only used if <see cref="FilterStrength"/> &gt; 0 or <see cref="Autofilter"/> &gt; 0.
        /// </summary>
        public int FilterType;

        /// <summary>Auto adjust filter's strength [0 = off, 1 = on].</summary>
        public int Autofilter;

        /// <summary>
        /// Algorithm for encoding the alpha plane (0 = none, 1 = compressed with WebP lossless).
        /// Default is 1.
        /// </summary>
        public int AlphaCompression;

        /// <summary>
        /// Predictive filtering method for alpha plane. 0: none, 1: fast, 2: best.
        /// Default is 1.
        /// </summary>
        public int AlphaFiltering;

        /// <summary>Between 0 (smallest size) and 100 (lossless). Default is 100.</summary>
        public int AlphaQuality;

        /// <summary>Number of entropy-analysis passes (in [1..10]).</summary>
        public int Pass;

        /// <summary>If true, export the compressed picture back. In-loop filtering is not applied.</summary>
        public int ShowCompressed;

        /// <summary>Preprocessing filter (0 = none, 1 = segment-smooth).</summary>
        public int Preprocessing;

        /// <summary>
        /// log2(number of token partitions) in [0..3].
        /// Default is set to 0 for easier progressive decoding.
        /// </summary>
        public int Partitions;

        /// <summary>
        /// Quality degradation allowed to fit the 512k limit on prediction modes coding
        /// (0 = no degradation, 100 = maximum possible degradation).
        /// </summary>
        public int PartitionLimit;

        /// <summary>If true, compression parameters will be remapped to better match the expected output size from JPEG compression.</summary>
        public int EmulateJpegSize;

        /// <summary>If non-zero, try and use multi-threaded encoding.</summary>
        public int ThreadLevel;

        /// <summary>If set, reduce memory usage (but increase CPU use).</summary>
        public int LowMemory;

        /// <summary>Near lossless encoding [0 = max loss .. 100 = off (default)].</summary>
        public int NearLossless;

        /// <summary>If non-zero, preserve the exact RGB values under transparent area. Otherwise, discard this invisible RGB information for better compression.</summary>
        public int Exact;

        /// <summary>Reserved for future lossless feature.</summary>
        public int UseDeltaPalette;

        /// <summary>If needed, use sharp (and slow) RGB-&gt;YUV conversion.</summary>
        public int UseSharpYuv;

        /// <summary>Minimum permissible quality factor.</summary>
        public int Qmin;

        /// <summary>Maximum permissible quality factor.</summary>
        public int Qmax;
    }
}