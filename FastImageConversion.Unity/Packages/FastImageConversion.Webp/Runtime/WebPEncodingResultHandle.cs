using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class WebPEncodingResultHandle : EncodingResultHandle
    {
        private WebpEncodingResult _source;

        internal unsafe WebPEncodingResultHandle(WebpEncodingResult encodingResult) :
            base((IntPtr)encodingResult.output.ptr, true)
        {
            _source = encodingResult;
        }

        public override unsafe NativeArray<byte> AsNativeArray()
        {
            return CreateView(_source.output.ptr, _source.output.length);
        }

        protected override unsafe bool ReleaseNativeMemory()
        {
            if (_source.output.ptr == null) return true;
            NativeMethods.fic_webp_dispose(_source);
            _source = default;
            return true;
        }
    }
}
