using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class PngDecodingResultHandle : DecodingResultHandle
    {
        public override int Width => (int)_source.width;
        public override int Height => (int)_source.height;

        private PngDecodingResult _source;

        internal unsafe PngDecodingResultHandle(PngDecodingResult decodingResult) :
            base((IntPtr)decodingResult.output.ptr, true)
        {
            _source = decodingResult;
        }

        public override unsafe NativeArray<byte> AsNativeArray()
        {
            return CreateView(_source.output.ptr, _source.output.length);
        }

        protected override unsafe bool ReleaseNativeMemory()
        {
            if (_source.output.ptr == null) return false;
            NativeMethods.fic_png_decode_dispose(_source);
            _source = default;
            return true;
        }
    }
}
