using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using FastImageConversion;

public class ImageConversionTests
{
    const int Width = 360;
    const int Height = 280;

    NativeArray<byte> _source;

    [OneTimeSetUp]
    public void SetUp()
    {
        _source = TestImage.Generate(Width, Height, Allocator.Persistent);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _source.Dispose();
    }

    [Test]
    public void Png_EncodeDecode_Roundtrip()
    {
        using var encoded = Png.Encode(_source, Width, Height);
        Assert.That(encoded.AsNativeArray().Length, Is.GreaterThan(0));

        using var decoded = Png.Decode(encoded.AsSpan());
        Assert.That(decoded.Width, Is.EqualTo(Width));
        Assert.That(decoded.Height, Is.EqualTo(Height));
        CollectionAssert.AreEqual(_source.ToArray(), decoded.ToArray());
    }

    [Test]
    public void Png_Info_ReturnsDimensions()
    {
        using var encoded = Png.Encode(_source, Width, Height);
        var meta = Png.Info(encoded.AsSpan());
        Assert.That(meta.Width, Is.EqualTo(Width));
        Assert.That(meta.Height, Is.EqualTo(Height));
    }

    [Test]
    public void FPng_EncodeDecode_Roundtrip()
    {
        using var encoded = FPng.Encode(_source, Width, Height);
        Assert.That(encoded.AsNativeArray().Length, Is.GreaterThan(0));

        using var decoded = FPng.Decode(encoded.AsSpan());
        Assert.That(decoded.Width, Is.EqualTo(Width));
        Assert.That(decoded.Height, Is.EqualTo(Height));
        CollectionAssert.AreEqual(_source.ToArray(), decoded.ToArray());
    }

    [Test]
    public void Png_CanDecode_FPngOutput()
    {
        // fpng output is readable by the general-purpose decoder
        using var encoded = FPng.Encode(_source, Width, Height);
        using var decoded = Png.Decode(encoded.AsSpan());
        CollectionAssert.AreEqual(_source.ToArray(), decoded.ToArray());
    }

    [Test]
    public void FPng_TryDecode_NonFPngInput_ReturnsFalse()
    {
        // PNGs written by other encoders (image-rs) are reported as NotFPng
        using var encoded = Png.Encode(_source, Width, Height);
        var result = FPng.TryDecode(encoded.AsSpan(), out var decoded);
        Assert.That(result, Is.False);
        Assert.That(decoded, Is.Null);
    }

    [Test]
    public void FPng_Decode_NonFPngInput_ThrowsNotFPng()
    {
        using var encoded = Png.Encode(_source, Width, Height);
        var bytes = encoded.ToArray();
        var ex = Assert.Throws<FPngDecodingException>(() =>
        {
            FPng.Decode(bytes);
        });
        Assert.That(ex.Status, Is.EqualTo(FPngDecodeStatus.NotFPng));
    }

    [Test]
    public void Png_Decode_InvalidData_Throws()
    {
        var junk = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        Assert.Throws<PngDecodingException>(() =>
        {
            Png.Decode(junk);
        });
    }

    [Test]
    public void Png_Encode_TooSmallBuffer_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Png.Encode(new byte[16], Width, Height);
        });
    }

    [Test]
    public void Webp_EncodeDecode_Lossless_Roundtrip()
    {
        var config = WebP.CreateLosslessConfig();

        using var encoded = WebP.Encode(_source, Width, Height, config);
        Assert.That(encoded.AsNativeArray().Length, Is.GreaterThan(0));

        var meta = WebP.Info(encoded.AsSpan());
        Assert.That(meta.Width, Is.EqualTo(Width));
        Assert.That(meta.Height, Is.EqualTo(Height));
        Assert.That(meta.Format, Is.EqualTo(WebPFormat.Lossless));

        using var decoded = WebP.Decode(encoded.AsSpan());
        Assert.That(decoded.Width, Is.EqualTo(Width));
        Assert.That(decoded.Height, Is.EqualTo(Height));
        CollectionAssert.AreEqual(_source.ToArray(), decoded.ToArray());
    }

    [Test]
    public void Webp_EncodeDecode_Lossy_Roundtrip()
    {
        using var encoded = WebP.Encode(_source, Width, Height);
        using var decoded = WebP.Decode(encoded.AsSpan());
        Assert.That(decoded.AsNativeArray().Length, Is.EqualTo(Width * Height * 4));
    }

    [Test]
    public void Texture_EncodeTexture_DecodeToTexture_Roundtrip()
    {
        // DecodeToTexture and EncodeTexture flip vertically in opposite directions,
        // so a full cycle must reproduce the original pixels exactly (PNG is lossless)
        using var encoded = Png.Encode(_source, Width, Height);
        var texture = Png.DecodeToTexture(encoded.AsSpan());
        try
        {
            Assert.That(texture.width, Is.EqualTo(Width));
            Assert.That(texture.height, Is.EqualTo(Height));

            var reencoded = FPng.EncodeTexture(texture);
            using var redecoded = Png.Decode(reencoded);
            CollectionAssert.AreEqual(_source.ToArray(), redecoded.ToArray());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}

public static class TestImage
{
    /// <summary>
    /// Generates a deterministic RGBA8 test image imitating a photo-like thumbnail
    /// (gradients + a shape + pseudo noise) so that it compresses similarly to real images.
    /// </summary>
    public static NativeArray<byte> Generate(int width, int height, Allocator allocator)
    {
        var data = new NativeArray<byte>(width * height * 4, allocator);
        uint rng = 0x12345678;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;

                // background gradient
                var r = x * 255 / width;
                var g = y * 255 / height;
                var b = (x + y) * 255 / (width + height);

                // a circle
                var dx = x - width / 2f;
                var dy = y - height / 2f;
                if (dx * dx + dy * dy < width * height / 8f)
                {
                    r = 255 - r;
                    b = 128;
                }

                // deterministic pseudo noise (xorshift)
                rng ^= rng << 13;
                rng ^= rng >> 17;
                rng ^= rng << 5;
                var noise = (int)(rng & 15) - 8;

                data[i + 0] = (byte)Mathf.Clamp(r + noise, 0, 255);
                data[i + 1] = (byte)Mathf.Clamp(g + noise, 0, 255);
                data[i + 2] = (byte)Mathf.Clamp(b + noise, 0, 255);
                data[i + 3] = 255;
            }
        }
        return data;
    }
}
