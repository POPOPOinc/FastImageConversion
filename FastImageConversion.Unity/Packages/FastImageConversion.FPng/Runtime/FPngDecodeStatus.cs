namespace FastImageConversion
{
    /// <summary>
    /// fpng::FPNG_DECODE_* に対応するステータスコード
    /// </summary>
    public enum FPngDecodeStatus
    {
        Success = 0,
        /// <summary>
        /// 有効なPNGだが、fpngが出力したものではない。
        /// 汎用PNGデコーダー (FastImageConversion.Png) へフォールバックすること。
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
