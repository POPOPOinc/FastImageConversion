using System;

namespace FastImageConversion
{
    /// <summary>
    /// libwebp decoding error codes (VP8_STATUS_*).
    /// </summary>
    public enum WebPDecodingError
    {
        None = 0,
        OutOfMemory = 1,
        InvalidParam = 2,
        BitstreamError = 3,
        UnsupportedFeature = 4,
        Suspended = 5,
        UserAbort = 6,
        NotEnoughData = 7,
    }

    public class WebPDecodingException : Exception
    {
        public WebPDecodingError Error { get; }

        public WebPDecodingException(WebPDecodingError error)
            : base($"Failed to decode WebP: {error}")
        {
            Error = error;
        }
    }
}
