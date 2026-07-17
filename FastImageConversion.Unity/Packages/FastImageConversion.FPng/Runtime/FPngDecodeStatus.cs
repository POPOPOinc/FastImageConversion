namespace FastImageConversion
{
    /// <summary>
    /// fpng decode status codes (fpng::FPNG_DECODE_*).
    /// </summary>
    public enum FPngDecodeStatus
    {
        Success = 0,
        /// <summary>
        /// The input is a valid PNG file, but it was not written by fpng.
        /// Fall back to a general-purpose PNG decoder (FastImageConversion.Png).
        /// </summary>
        NotFPng = 1,
        InvalidArg = 2,
        FailedNotPng = 3,
        FailedHeaderCrc32 = 4,
        FailedInvalidDimensions = 5,
        FailedDimensionsTooLarge = 6,
        FailedChunkParsing = 7,
        FailedInvalidIdat = 8,
    }
}
