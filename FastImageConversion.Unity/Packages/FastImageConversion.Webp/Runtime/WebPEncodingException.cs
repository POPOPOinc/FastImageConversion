using System;

namespace FastImageConversion
{
    public class WebpEncodingException : Exception
    {
        public WebpEncodingException(string message) : base(message)
        {
        }
        
        internal WebpEncodingException(WebpEncodingErrorCode errorCode) : base(errorCode.ToString())
        {
        }
    }
}
