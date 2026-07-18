using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using FastImageConversion;

namespace FastImageConversion.Tests
{
    [TestFixture(360, 280)]
    [TestFixture(1024, 1024)]
    [TestFixture(2048, 2048)]
    [TestFixture(4096, 4096)]
    public class ImageConversionPerformanceTests
    {
        readonly int Width;
        readonly int Height;

        const int WarmupCount = 5;
        const int MeasurementCount = 20;

        NativeArray<byte> _source;

        // Decode inputs
        byte[] _pngBytes;
        byte[] _fpngBytes;
        byte[] _webpBytes;

        public ImageConversionPerformanceTests(int width, int height)
        {
            Width = width;
            Height = height;
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            _source = TestImage.Generate(Width, Height, Allocator.Persistent);

            using var png = Png.Encode(_source, Width, Height);
            _pngBytes = png.ToArray();
            using var fpng = FPng.Encode(_source, Width, Height);
            _fpngBytes = fpng.ToArray();
            using var webp = WebP.Encode(_source, Width, Height);
            _webpBytes = webp.ToArray();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _source.Dispose();
        }

        [Test, Performance]
        public void Encode_UnityImageConversion_Png()
        {
            Measure.Method(() =>
                {
                    var encoded = ImageConversion.EncodeNativeArrayToPNG(
                        _source, GraphicsFormat.R8G8B8A8_UNorm, (uint)Width, (uint)Height);
                    encoded.Dispose();
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();

            ReportEncodedSize(() =>
            {
                using var encoded = ImageConversion.EncodeNativeArrayToPNG(
                    _source, GraphicsFormat.R8G8B8A8_UNorm, (uint)Width, (uint)Height);
                return encoded.Length;
            });
        }

        [Test, Performance]
        public void Encode_Fic_Png()
        {
            Measure.Method(() =>
                {
                    using var encoded = Png.Encode(_source, Width, Height);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();

            ReportEncodedSize(() => _pngBytes.Length);
        }

        [Test, Performance]
        public void Encode_Fic_FPng()
        {
            Measure.Method(() =>
                {
                    using var encoded = FPng.Encode(_source, Width, Height);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();

            ReportEncodedSize(() => _fpngBytes.Length);
        }

        [Test, Performance]
        public void Encode_Fic_Webp()
        {
            Measure.Method(() =>
                {
                    using var encoded = WebP.Encode(_source, Width, Height);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();

            ReportEncodedSize(() => _webpBytes.Length);
        }

        [Test, Performance]
        public void Decode_UnityImageConversion_Png()
        {
            var texture = new Texture2D(2, 2);
            Measure.Method(() =>
                {
                    ImageConversion.LoadImage(texture, _pngBytes);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        [Test, Performance]
        public void Decode_Fic_Png()
        {
            Measure.Method(() =>
                {
                    using var decoded = Png.Decode(_pngBytes);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();
        }

        [Test, Performance]
        public void Decode_Fic_FPng()
        {
            Measure.Method(() =>
                {
                    using var decoded = FPng.Decode(_fpngBytes);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();
        }

        [Test, Performance]
        public void Decode_Fic_Webp()
        {
            Measure.Method(() =>
                {
                    using var decoded = WebP.Decode(_webpBytes);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .Run();
        }

        static void ReportEncodedSize(Func<int> getSize)
        {
            Measure.Custom(new SampleGroup("EncodedSize", SampleUnit.Byte), getSize());
        }
    }
}
