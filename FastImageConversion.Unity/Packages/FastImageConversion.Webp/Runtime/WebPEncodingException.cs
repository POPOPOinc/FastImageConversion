using System;

namespace FastImageConversion
{
    /// <summary>
    /// libwebp encoding error codes (VP8_ENC_ERROR_*).
    /// </summary>
    public enum WebPEncodingError
    {
        None = 0,
        OutOfMemory = 1,
        BitstreamOutOfMemory = 2,
        NullParameter = 3,
        InvalidConfiguration = 4,
        BadDimension = 5,
        Partition0Overflow = 6,
        PartitionOverflow = 7,
        BadWrite = 8,
        FileTooBig = 9,
        UserAbort = 10,
        Last = 11,
    }

    public class WebPEncodingException : Exception
    {
        public WebPEncodingError Error { get; }

        public WebPEncodingException(WebPEncodingError error)
            : base($"Failed to encode WebP: {error}")
        {
            Error = error;
        }
    }
}
