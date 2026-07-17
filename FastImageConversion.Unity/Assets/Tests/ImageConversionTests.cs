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

        using var decoded = Png.Decode(encoded.AsNativeArray().AsReadOnlySpan());
        Assert.That(decoded.Width, Is.EqualTo(Width));
        Assert.That(decoded.Height, Is.EqualTo(Height));
        CollectionAssert.AreEqual(_source.ToArray(), decoded.AsNativeArray().ToArray());
    }

    [Test]
    public void FPng_EncodeDecode_Roundtrip()
    {
        using var encoded = FPng.Encode(_source, Width, Height);
        Assert.That(encoded.AsNativeArray().Length, Is.GreaterThan(0));

        using var decoded = FPng.Decode(encoded.AsNativeArray().AsReadOnlySpan());
        Assert.That(decoded.Width, Is.EqualTo(Width));
        Assert.That(decoded.Height, Is.EqualTo(Height));
        CollectionAssert.AreEqual(_source.ToArray(), decoded.AsNativeArray().ToArray());
    }

    [Test]
    public void Png_CanDecode_FPngOutput()
    {
        // fpngの出力は汎用PNGデコーダーで読める
        using var encoded = FPng.Encode(_source, Width, Height);
        using var decoded = Png.Decode(encoded.AsNativeArray().AsReadOnlySpan());
        CollectionAssert.AreEqual(_source.ToArray(), decoded.AsNativeArray().ToArray());
    }

    [Test]
    public void FPng_Decode_NonFPngInput_ThrowsNotFPng()
    {
        // fpng以外(image-rs)が出力したPNGは NotFPng になる
        using var encoded = Png.Encode(_source, Width, Height);
        var bytes = encoded.AsNativeArray().ToArray();
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
    public void Webp_EncodeDecode_Lossless_Roundtrip()
    {
        var config = Webp.CreateConfig(WebPPreset.Picture, 100f);
        config.Lossless = 1;
        config.Exact = 1;

        using var encoded = Webp.Encode(_source.AsReadOnlySpan(), Width, Height, config);
        Assert.That(encoded.AsNativeArray().Length, Is.GreaterThan(0));

        var meta = Webp.Info(encoded.AsNativeArray().AsReadOnlySpan());
        Assert.That(meta.Width, Is.EqualTo(Width));
        Assert.That(meta.Height, Is.EqualTo(Height));

        using var decoded = Webp.Decode(encoded.AsNativeArray().AsReadOnlySpan());
        CollectionAssert.AreEqual(_source.ToArray(), decoded.AsNativeArray().ToArray());
    }

    [Test]
    public void Webp_EncodeDecode_Lossy_Roundtrip()
    {
        using var encoded = Webp.Encode(_source.AsReadOnlySpan(), Width, Height);
        using var decoded = Webp.Decode(encoded.AsNativeArray().AsReadOnlySpan());
        Assert.That(decoded.AsNativeArray().Length, Is.EqualTo(Width * Height * 4));
    }
}

public static class TestImage
{
    /// <summary>
    /// サムネイル画像を模した決定論的なテスト画像 (RGBA8) を生成する。
    /// グラデーション + 図形 + 擬似ノイズで、実写画像に近い圧縮率になるようにしている。
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

                // 背景グラデーション
                var r = x * 255 / width;
                var g = y * 255 / height;
                var b = (x + y) * 255 / (width + height);

                // 円形の図形
                var dx = x - width / 2f;
                var dy = y - height / 2f;
                if (dx * dx + dy * dy < width * height / 8f)
                {
                    r = 255 - r;
                    b = 128;
                }

                // 実写画像を模した擬似ノイズ (xorshift、決定論的)
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
