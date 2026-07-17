using System;

namespace FastImageConversion
{
    public class WebpDecodingException : Exception
    {
        public WebpDecodingException(string message) : base(message)
        {
        }
        
        internal WebpDecodingException(WebpDecodingErrorCode errorCode) : base(errorCode.ToString())
        {
        }
    }
}
