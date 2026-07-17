using System;

namespace FastImageConversion
{
    public class FPngDecodingException : Exception
    {
        public FPngDecodeStatus Status { get; }

        public FPngDecodingException(FPngDecodeStatus status)
            : base($"Failed to decode with fpng: {status}")
        {
            Status = status;
        }
    }
}
