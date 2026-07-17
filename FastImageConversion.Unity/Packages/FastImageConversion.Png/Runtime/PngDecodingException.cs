using System;

namespace FastImageConversion
{
    public class PngDecodingException : Exception
    {
        public PngDecodingException(string message) : base(message)
        {
        }
    }
}
