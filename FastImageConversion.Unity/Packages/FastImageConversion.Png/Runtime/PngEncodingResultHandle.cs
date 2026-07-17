using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class PngEncodingResultHandle : EncodingResultHandle
    {
        private PngEncodingResult _source;

        internal unsafe PngEncodingResultHandle(PngEncodingResult encodingResult) :
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
            if (_source.output.ptr == null) return false;
            NativeMethods.fic_png_dispose(_source);
            _source = default;
            return true;
        }
    }
}
