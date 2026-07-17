# FastImageConversion

[English](./README.md) | 日本語

Unity 向けの高速な画像エンコード / デコードプラグイン集です。薄いネイティブライブラリとして実装されています。

`UnityEngine.ImageConversion` と違い、すべての API を **メインスレッド以外から** 呼び出すことができ、エンコード・デコードともに大幅に高速です。

## 機能

|                | Encode             | Decode |
|----------------|--------------------|--------|
| WebP           | :white_check_mark: | :white_check_mark: |
| PNG (image-rs) | :white_check_mark: | :white_check_mark: |
| PNG (fpng)     | :white_check_mark: | :white_check_mark: (fpngが出力したファイルのみ) |
| JPEG           | :x:                | :x: |

- **メインスレッド非依存**: Unity メインスレッドに依存しないため、ワーカースレッドや自前のジョブからエンコード・デコードできます
- **PNG は2種類の実装から選択可能**:
    - [image-rs/png](https://github.com/image-rs/image-png) — pure Rust の汎用エンコーダー/デコーダー
    - [fpng](https://github.com/richgel999/fpng) — SSE 最適化された非常に高速な C++ エンコーダー。デコーダーは fpng 自身が出力した PNG のみ対応のため、任意の PNG は image-rs 側へフォールバックしてください
- **WebP** — エンコード速度に最適化した libwebp ビルド。lossy / lossless 両対応
- **ゼロコピーな結果アクセス** — エンコード/デコード結果はネイティブメモリ上にあり、`NativeArray<byte>` のビューとして参照できます。所有権は `SafeHandle` で管理されます

## パフォーマンス

360x280 の RGBA8 テスト画像 (実写調の合成画像)、Apple M4、Unity 6000.3 エディタ上、20回計測の中央値です。
[Unity Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.1/manual/index.html) で計測しています — 再現するには `Assets/Tests/ImageConversionPerformanceTests.cs` を実行してください。

### Encode

|                                     | latency (median) | encoded size |
|-------------------------------------|-----------------|--------------|
| UnityEngine.ImageConversion (PNG)   | 3.61 ms         | 152,839 B    |
| FastImageConversion PNG (image-rs)  | **0.54 ms**     | 228,182 B    |
| FastImageConversion PNG (fpng)      | **0.56 ms**     | 246,197 B    |
| FastImageConversion WebP (lossy, デフォルト設定) | **0.86 ms** | 4,360 B |

### Decode

|                                     | latency (median) |
|-------------------------------------|-----------------|
| UnityEngine ImageConversion.LoadImage (PNG) | 1.78 ms |
| FastImageConversion PNG (image-rs)  | **0.58 ms**     |
| FastImageConversion PNG (fpng)      | **0.72 ms**     |
| FastImageConversion WebP            | **0.24 ms**     |

補足:

- PNG エンコーダーは **圧縮率よりも速度を優先** した設定 (最速の圧縮レベル) のため、Unity デフォルトの PNG 出力よりサイズが大きくなります。サイズが重要な場合は、同等の速度で劇的に小さい WebP を検討してください
- `ImageConversion.LoadImage` は `Texture2D` へのアップロードまで行うため、デコードの比較は厳密に同条件ではありません。が、代替として実際に使う API との比較にはなっています

## 対応プラットフォーム

以下のビルド済みバイナリを同梱しています。

| Plugin | linux-x64 | windows-x64 | macOS-arm64 | iOS-arm64 | Android-arm64 |
|--------|-----------|-------------|-------------|-----------|---------------|
| PNG (image-rs) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| WebP   | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| PNG (fpng) | :white_check_mark: | :white_check_mark: | :white_check_mark: | :white_check_mark: | :x: |

その他のターゲットはソースからビルドできます ([ビルド](#ソースからのビルド) 参照)。

## インストール

必要なパッケージを UPM (git URL) で追加してください。`FastImageConversion.Core` はすべてのコーデックパッケージから必要とされます:

```
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Core
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Png
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.FPng
https://github.com/POPOPOinc/FastImageConversion.git?path=FastImageConversion.Unity/Packages/FastImageConversion.Webp
```

## 使い方

### ピクセルフォーマット

ネイティブプラグイン側が入力にとる画像データは、4バイト1組のピクセルデータが連続して並んだものです:

```
RGBARGBARGBA...
```

- R,G,B,A 各チャンネルは各1バイト (0-255)
- 各種画像ライブラリの慣習に合わせて、原点は **左上** を期待します

Unity の `GraphicsFormat.R8G8B8A8_UNorm` 形式とほぼ同一ですが、Unity のテクスチャは原点が **左下** なので、エンコード前に上下を反転させてください:

```csharp
using FastImageConversion;

NativeArray<byte> pixels = texture.GetRawTextureData<byte>();
PixelSorting.FlipVerticalInplace(pixels, texture.width, texture.height);
```

### エンコード

```csharp
using FastImageConversion;

// PNG (fpng)
using (var encoded = FPng.Encode(pixels, width, height))
{
    File.WriteAllBytes(path, encoded.AsNativeArray().ToArray());
}

// PNG (image-rs)
using (var encoded = Png.Encode(pixels, width, height))
{
    File.WriteAllBytes(path, encoded.AsNativeArray().ToArray());
}

// WebP
var config = Webp.CreateConfig(WebPPreset.Picture, qualityFactor: 75f);
using (var encoded = Webp.Encode(pixels.AsReadOnlySpan(), width, height, config))
{
    File.WriteAllBytes(path, encoded.AsNativeArray().ToArray());
}
```

結果のハンドルはネイティブメモリを所有しており、`using` などで Dispose すると解放されます。
`AsNativeArray()` はそのメモリへのゼロコピーのビューです。ハンドルの寿命を超えて使う場合はコピーしてください。

### デコード

すべてのデコーダーは RGBA8 (`GraphicsFormat.R8G8B8A8_UNorm` 相当) のピクセルデータを出力します:

```csharp
// PNG (任意のPNGファイル)
using (var decoded = Png.Decode(pngBytes))
{
    var texture = new Texture2D((int)decoded.Width, (int)decoded.Height, TextureFormat.RGBA32, false);
    texture.LoadRawTextureData(decoded.AsNativeArray());
    texture.Apply();
}

// WebP
using (var decoded = Webp.Decode(webpBytes))
{
    var meta = decoded.Meta; // width / height など
    // ...
}
```

`FPng.Decode` は fpng 自身がエンコードした PNG のみ読めます。任意の PNG は汎用デコーダーへフォールバックしてください:

```csharp
try
{
    using var decoded = FPng.Decode(bytes);
    // ...
}
catch (FPngDecodingException e) when (e.Status == FPngDecodeStatus.NotFPng)
{
    using var decoded = Png.Decode(bytes);
    // ...
}
```

## プロジェクト構造

```
.
├── FastImageConversion.Unity     # パッケージとテストをホストする Unity プロジェクト
│   ├── Packages
│   │   ├── FastImageConversion.Core   # 共通ヘルパー (PixelSorting、ハンドル基底)
│   │   ├── FastImageConversion.Png    # image-rs ベースの PNG エンコーダー/デコーダー
│   │   ├── FastImageConversion.FPng   # fpng ベースの PNG エンコーダー/デコーダー
│   │   └── FastImageConversion.Webp   # libwebp ベースの WebP エンコーダー/デコーダー
│   └── Assets/Tests              # 正しさ + パフォーマンステスト
└── fast_image_conversion_native  # ネイティブプラグインを生成する Rust ワークスペース
    ├── fic_png                   # image-rs ラッパー
    ├── fic_fpng                  # fpng (C++、git submodule) ラッパー
    ├── fic_webp                  # libwebp ラッパー
    └── cli                       # ローカルテスト用の小さな CLI
```

各ネイティブプラグインは Rust のビルドツールチェインでビルドされます。C# バインディング (`NativeMethods.g.cs`) はビルド時に [csbindgen](https://github.com/Cysharp/csbindgen) で自動生成されます。

## ソースからのビルド

fpng は git submodule として参照しているため、`--recursive` 付きで clone するか、以下を実行してください:

```bash
git submodule update --init
```

make でプラグインをビルドし、Unity パッケージへ配置します:

```bash
cd fast_image_conversion_native

# 全ターゲットをビルド
make

# ターゲットを指定してビルド
make linux-x64
make windows-x64
make macos-arm64
make ios-arm64
make android-arm64
```

### クロスビルドが通らない場合

fpng のビルドには C++ のクロスツールチェインが必要で、rustup だけではサポートされません。

- **macOS から linux-x64**: 同梱の Docker コンテナ内でビルドしてください:
  ```bash
  cd build
  docker compose run --rm rust-builder-x64 bash
  # make build-linux-x64
  # exit
  make install-linux-x64
  ```
- **macOS から windows-x64**: mingw-w64 が必要です (`brew install mingw-w64`)
- **android-arm64**: Android NDK が必要です (`cargo install cargo-ndk`)。fpng プラグインは現在 Android 向けにはビルドしていません

## テストの実行

`FastImageConversion.Unity` を開いて Test Runner から EditMode テストを実行するか、コマンドラインから実行します:

```bash
Unity -batchmode -projectPath FastImageConversion.Unity \
  -runTests -testPlatform EditMode \
  -testResults results.xml -perfTestResults perf.json
```

## License

MIT License — [LICENSE](./LICENSE) を参照してください。

このリポジトリはサードパーティソフトウェア (fpng, libwebp, image-rs など) を同梱・リンクしています — [THIRDPARTY_NOTICES.md](./THIRDPARTY_NOTICES.md) を参照してください。
