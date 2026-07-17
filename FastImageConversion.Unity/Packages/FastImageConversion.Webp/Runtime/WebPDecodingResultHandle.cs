using System;
using Unity.Collections;

namespace FastImageConversion
{
    public class WebPDecodingResultHandle : DecodingResultHandle
    {
        public override int Width => _source.meta.Width;
        public override int Height => _source.meta.Height;

        public WebPMeta Meta => new(_source.meta);

        private WebpDecodingResult _source;

        internal WebPDecodingResultHandle(WebpDecodingResult decodingResult) :
            base(decodingResult.output.U.RGBA.RGBA, true)
        {
            _source = decodingResult;
        }

        public override unsafe NativeArray<byte> AsNativeArray()
        {
            var rgba = _source.output.U.RGBA;
            return CreateView((void*)rgba.RGBA, (int)rgba.Size);
        }

        protected override bool ReleaseNativeMemory()
        {
            if (_source.output.U.RGBA.RGBA == IntPtr.Zero) return true;
            NativeMethods.fic_webp_decode_dispose(_source);
            _source = default;
            return true;
        }
    }
}
