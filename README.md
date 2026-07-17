# FastImageConversion

English | [日本語](./README.ja.md)

Fast image encoding / decoding plugins for Unity, implemented as thin native libraries.

Unlike `UnityEngine.ImageConversion`, every API is callable **from any thread** (not just the Unity main thread), and both encoding and decoding are considerably faster.

## Features

|                | Encode             | Decode |
|----------------|--------------------|--------|
| WebP           | :white_check_mark: | :white_check_mark: |
| PNG (image-rs) | :white_check_mark: | :white_check_mark: |
| PNG (fpng)     | :white_check_mark: | :white_check_mark: (fpng-encoded files only) |
| JPEG           | :x:                | :x: |

- **Off-main-thread**: no dependency on the Unity main thread. Encode/decode from worker threads or your own job code
- **Two PNG implementations to choose from**:
    - [image-rs/png](https://github.com/image-rs/image-png) — pure Rust, general-purpose decoder/encoder
    - [fpng](https://github.com/richgel999/fpng) — very fast SSE-optimized C++ encoder. Its decoder only reads PNGs produced by fpng itself; fall back to the image-rs decoder for arbitrary PNGs
- **WebP** — libwebp build tuned for encoding speed. Lossy and lossless are both supported
- **Zero-copy results** — encoded/decoded results live in native memory and are exposed as `NativeArray<byte>` views; ownership is managed by `SafeHandle`

## Performance

Median of 20 runs, 360x280 RGBA8 synthetic photo-like test image, Apple M4, inside the Unity 6000.3 Editor.
Measured with [Unity Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.1/manual/index.html) — see `Assets/Tests/ImageConversionPerformanceTests.cs` to reproduce.

### Encode

vs `UnityEngine.ImageConversion.EncodeNativeArrayToPNG`:

|                                     | latency (median) | vs UnityEngine | encoded size |
|-------------------------------------|-----------------|----------------|--------------|
| UnityEngine.ImageConversion (PNG)   | 3.61 ms         | 1.0x (baseline) | 152,839 B    |
| FastImageConversion PNG (image-rs)  | **0.54 ms**     | **6.7x faster** | 228,182 B    |
| FastImageConversion PNG (fpng)      | **0.56 ms**     | **6.4x faster** | 246,197 B    |
| FastImageConversion WebP (lossy, default config) | **0.86 ms** | **4.2x faster** | 4,360 B |

### Decode

vs `UnityEngine.ImageConversion.LoadImage`:

|                                     | latency (median) | vs UnityEngine |
|-------------------------------------|-----------------|----------------|
| UnityEngine ImageConversion.LoadImage (PNG) | 1.78 ms | 1.0x (baseline) |
| FastImageConversion PNG (image-rs)  | **0.58 ms**     | **3.1x faster** |
| FastImageConversion PNG (fpng)      | **0.72 ms**     | **2.5x faster** |
| FastImageConversion WebP            | **0.24 ms**     | **7.4x faster** |

Notes:

- The PNG encoders are configured for **speed over compression ratio** (fastest compression level), so their output is larger than Unity's default PNG output. If output size matters, WebP is dramatically smaller at comparable speed
- `ImageConversion.LoadImage` also uploads the result to a `Texture2D`, so the decode comparison is not strictly apples-to-apples — but it is the API you would otherwise use

## Supported platforms

Prebuilt binaries are included for:

| Plugin | linux-x64 | windows-x64 | macOS-arm64 | iOS-arm64 | Android-arm64 |
|--------|-----------|-------------|-------------|-----------|---------------|
| PNG (image-rs) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| WebP   | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| PNG (fpng) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :x: |

Other targets can be produced from source (see [Building](#building-from-source)).

## Installation

Add the packages you need via UPM (git URL). `FastImageConversion.Core` is required by all codec packages:

```
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Core
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Png
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.FPng
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Webp
```

## Usage

### Pixel format

The native plugins take pixel data as consecutive 4-byte RGBA pixels:

```
RGBARGBARGBA...
```

- 1 byte per channel (0-255)
- The origin is expected to be **top-left**, following common image library conventions

This is almost identical to Unity's `GraphicsFormat.R8G8B8A8_UNorm`, but Unity textures have their origin at the **bottom-left**, so a vertical flip is needed in between. The Texture2D helpers (`EncodeTexture` / `DecodeToTexture` / `ToTexture2D`) handle this automatically; for the low-level APIs, `PixelUtility` provides the conversions:

```csharp
using FastImageConversion;

// Readable Texture2D -> RGBA8 pixels with a top-left origin (flip included)
using var pixels = PixelUtility.GetPixelsTopLeft(texture, Allocator.Temp);

// or flip an existing RGBA8 buffer in place
PixelUtility.FlipVertically(pixels, width, height);
```

### Encode

The simplest form — encode a readable `Texture2D` (flip handled internally, main thread only):

```csharp
using FastImageConversion;

File.WriteAllBytes(path, FPng.EncodeTexture(texture));         // PNG (fpng)
File.WriteAllBytes(path, Png.EncodeTexture(texture));          // PNG (image-rs)
File.WriteAllBytes(path, WebP.EncodeTexture(texture));         // WebP
```

The low-level APIs take RGBA8 pixels (`ReadOnlySpan<byte>` or `NativeArray<byte>`, top-left origin) and are callable from any thread:

```csharp
// PNG
using (var encoded = FPng.Encode(pixels, width, height))
{
    File.WriteAllBytes(path, encoded.ToArray());
}

// WebP with an explicit config
var config = WebP.CreateConfig(WebPPreset.Picture, qualityFactor: 75f);
// also: WebP.CreateFastConfig() (default) / WebP.CreateLosslessConfig()
using (var encoded = WebP.Encode(pixels, width, height, config))
{
    File.WriteAllBytes(path, encoded.ToArray());
}
```

The result handles own native memory; disposing them (e.g. with `using`) frees it.
`AsNativeArray()` / `AsSpan()` are zero-copy views into that memory; `ToArray()` copies it into a managed array.

### Decode

The simplest form — decode into a `Texture2D` (flip handled internally, main thread only):

```csharp
Texture2D texture = Png.DecodeToTexture(pngBytes);
Texture2D texture = WebP.DecodeToTexture(webpBytes);
```

The low-level APIs produce RGBA8 (`GraphicsFormat.R8G8B8A8_UNorm` compatible) pixel data with a top-left origin, callable from any thread:

```csharp
using (var decoded = Png.Decode(pngBytes))
{
    var width = decoded.Width;
    var height = decoded.Height;
    NativeArray<byte> rgba = decoded.AsNativeArray(); // zero-copy view

    // on the main thread, decoded.ToTexture2D() creates a Texture2D (flip included)
}

// Header-only peek without decoding pixels
PngMeta pngMeta = Png.Info(pngBytes);   // Width / Height
WebPMeta webpMeta = WebP.Info(webpBytes); // Width / Height / HasAlpha / Format
```

The fpng decoder only reads PNGs encoded by fpng itself. Use `TryDecode` to fall back to the general-purpose decoder:

```csharp
if (!FPng.TryDecode(bytes, out var decoded))
{
    decoded = Png.Decode(bytes); // not written by fpng — use the general-purpose decoder
}
using (decoded)
{
    // ...
}
```

## Project structure

```
.
├── FastImageConversion.Unity     # Unity project hosting the packages and tests
│   ├── Packages
│   │   ├── FastImageConversion.Core   # shared helpers (PixelUtility, handle bases)
│   │   ├── FastImageConversion.Png    # image-rs based PNG encoder/decoder
│   │   ├── FastImageConversion.FPng   # fpng based PNG encoder/decoder
│   │   └── FastImageConversion.Webp   # libwebp based WebP encoder/decoder
│   └── Assets/Tests              # correctness + performance tests
└── fast_image_conversion_native  # Rust workspace producing the native plugins
    ├── fic_png                   # image-rs wrapper
    ├── fic_fpng                  # fpng (C++, git submodule) wrapper
    ├── fic_webp                  # libwebp wrapper
    └── cli                       # small CLI for local testing
```

Each native plugin is built with the Rust toolchain. C# bindings (`NativeMethods.g.cs`) are generated at build time by [csbindgen](https://github.com/Cysharp/csbindgen).

## Building from source

fpng is referenced as a git submodule — clone with `--recursive`, or run:

```bash
git submodule update --init
```

Then build and install the plugins into the Unity packages with make:

```bash
cd fast_image_conversion_native

# build all targets
make

# or a single target
make linux-x64
make windows-x64
make macos-arm64
make ios-arm64
make android-arm64
```

### If a cross build fails

fpng requires a C++ cross toolchain, which rustup alone does not provide.

- **linux-x64 from macOS**: build inside the provided Docker container:
  ```bash
  cd build
  docker compose run --rm rust-builder-x64 bash
  # make build-linux-x64
  # exit
  make install-linux-x64
  ```
- **windows-x64 from macOS**: requires mingw-w64 (`brew install mingw-w64`)
- **android-arm64**: requires the Android NDK (`cargo install cargo-ndk`); the fpng plugin is currently not built for Android

## Running the tests

Open `FastImageConversion.Unity` and run the EditMode tests from the Test Runner, or from the command line:

```bash
Unity -batchmode -projectPath FastImageConversion.Unity \
  -runTests -testPlatform EditMode \
  -testResults results.xml -perfTestResults perf.json
```

## License

MIT License — see [LICENSE](./LICENSE).

This repository bundles or links third-party software (fpng, libwebp, image-rs, and others) — see [THIRDPARTY_NOTICES.md](./THIRDPARTY_NOTICES.md).
