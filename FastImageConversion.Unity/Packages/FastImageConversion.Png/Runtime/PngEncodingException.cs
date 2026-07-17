using System;

namespace FastImageConversion
{
    public class PngEncodingException : Exception
    {
        public PngEncodingException(string message) : base(message)
        {
        }
    }
}
