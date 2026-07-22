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

RGBA8 のテスト画像 (実写調の合成画像)、Apple M4、Unity 6000.3 エディタ上、20回計測の中央値です。
[Unity Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.1/manual/index.html) で 360x280 / 1024x1024 / 2048x2048 / 4096x4096 の4サイズを計測しています — 再現するには `Assets/Tests/ImageConversionPerformanceTests.cs` を実行してください。以下は代表的な2サイズです。計測した全サイズを通して、エンコードはおおよそ 5〜7 倍、WebP デコードは最大で約 10 倍、`UnityEngine.ImageConversion` より高速でした。

### Encode

`UnityEngine.ImageConversion.EncodeNativeArrayToPNG` との比較:

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/benchmark_encode_dark.svg">
  <img src="docs/images/benchmark_encode.svg" alt="エンコードのレイテンシ比較グラフ: FastImageConversion は UnityEngine.ImageConversion の 4.3〜6.8 倍高速" width="760">
</picture>

<details>
<summary>数値 (エンコードサイズ含む)</summary>

**360x280 (サムネイル)**

|                                     | latency (median) | UnityEngine比 | encoded size |
|-------------------------------------|-----------------|----------------|--------------|
| UnityEngine.ImageConversion (PNG)   | 3.53 ms         | 1.0x (基準)    | 152,839 B    |
| FastImageConversion PNG (image-rs)  | **0.52 ms**     | **6.8x 高速**  | 228,182 B    |
| FastImageConversion PNG (fpng)      | **0.54 ms**     | **6.5x 高速**  | 246,197 B    |
| FastImageConversion WebP (lossy, デフォルト設定) | **0.83 ms** | **4.3x 高速** | 4,360 B |

**4096x4096 (大きな画像)**

|                                     | latency (median) | UnityEngine比 | encoded size |
|-------------------------------------|-----------------|----------------|--------------|
| UnityEngine.ImageConversion (PNG)   | 557.3 ms        | 1.0x (基準)    | 19,935,689 B |
| FastImageConversion PNG (image-rs)  | **108.4 ms**    | **5.1x 高速**  | 37,606,042 B |
| FastImageConversion PNG (fpng)      | **93.0 ms**     | **6.0x 高速**  | 40,360,776 B |
| FastImageConversion WebP (lossy, デフォルト設定) | **115.7 ms** | **4.8x 高速** | 225,230 B |

</details>

### Decode

`UnityEngine.ImageConversion.LoadImage` との比較:

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/images/benchmark_decode_dark.svg">
  <img src="docs/images/benchmark_decode.svg" alt="デコードのレイテンシ比較グラフ: FastImageConversion は UnityEngine.ImageConversion の 2.1〜9.9 倍高速" width="760">
</picture>

<details>
<summary>数値</summary>

**360x280 (サムネイル)**

|                                     | latency (median) | UnityEngine比 |
|-------------------------------------|-----------------|----------------|
| UnityEngine ImageConversion.LoadImage (PNG) | 1.71 ms | 1.0x (基準) |
| FastImageConversion PNG (image-rs)  | **0.59 ms**     | **2.9x 高速**  |
| FastImageConversion PNG (fpng)      | **0.70 ms**     | **2.5x 高速**  |
| FastImageConversion WebP            | **0.24 ms**     | **7.3x 高速**  |

**4096x4096 (大きな画像)**

|                                     | latency (median) | UnityEngine比 |
|-------------------------------------|-----------------|----------------|
| UnityEngine ImageConversion.LoadImage (PNG) | 272.7 ms | 1.0x (基準) |
| FastImageConversion PNG (image-rs)  | **96.1 ms**     | **2.8x 高速**  |
| FastImageConversion PNG (fpng)      | **128.9 ms**    | **2.1x 高速**  |
| FastImageConversion WebP            | **27.7 ms**     | **9.9x 高速**  |

</details>

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

Unity の `GraphicsFormat.R8G8B8A8_UNorm` 形式とほぼ同一ですが、Unity のテクスチャは原点が **左下** のため、間で上下反転が必要です。Texture2D ヘルパー (`EncodeTexture` / `DecodeToTexture` / `ToTexture2D`) はこれを自動で処理します。低レベル API を使う場合は `PixelUtility` で変換できます:

```csharp
using FastImageConversion;

// 読み取り可能な Texture2D → 左上原点の RGBA8 ピクセル (反転込み)
using var pixels = PixelUtility.GetPixelsTopLeft(texture, Allocator.Temp);

// あるいは既存の RGBA8 バッファをその場で上下反転
PixelUtility.FlipVertically(pixels, width, height);
```

### エンコード

もっとも簡単な形 — 読み取り可能な `Texture2D` をエンコード (反転は内部で処理、メインスレッド限定):

```csharp
using FastImageConversion;

File.WriteAllBytes(path, FPng.EncodeTexture(texture));         // PNG (fpng)
File.WriteAllBytes(path, Png.EncodeTexture(texture));          // PNG (image-rs)
File.WriteAllBytes(path, WebP.EncodeTexture(texture));         // WebP
```

なお、`EncodeTexture` は利便性のためにマネージド `byte[]` を返します — GC アロケーションが発生します。

低レベル API は RGBA8 ピクセル (`ReadOnlySpan<byte>` または `NativeArray<byte>`、左上原点) を受け取り、任意のスレッドから呼び出せます。エンコード結果はネイティブメモリ上にあるため、`AsSpan()` 経由で書き出せば**マネージドメモリの確保はゼロ**です:

```csharp
// PNG — ゼロコピーでファイルへ書き出し
using (var encoded = FPng.Encode(pixels, width, height))
using (var file = File.OpenWrite(path))
{
    file.Write(encoded.AsSpan());
}

// WebP (設定を明示する場合)
var config = WebP.CreateConfig(WebPPreset.Picture, qualityFactor: 75f);
// ほかに WebP.CreateFastConfig() (デフォルト) / WebP.CreateLosslessConfig() もあります
using (var encoded = WebP.Encode(pixels, width, height, config))
using (var file = File.OpenWrite(path))
{
    file.Write(encoded.AsSpan());
}
```

結果のハンドルはネイティブメモリを所有しており、`using` などで Dispose すると解放されます。
`AsNativeArray()` / `AsSpan()` はそのメモリへのゼロコピーのビューで、ハンドルを Dispose するまで有効です。
`ToArray()` は新しいマネージド配列へのコピー (GC アロケーション) なので、`byte[]` が本当に必要な場合のみ使ってください。

### デコード

もっとも簡単な形 — `Texture2D` へデコード (反転は内部で処理、メインスレッド限定):

```csharp
Texture2D texture = Png.DecodeToTexture(pngBytes);
Texture2D texture = WebP.DecodeToTexture(webpBytes);
```

低レベル API は RGBA8 (`GraphicsFormat.R8G8B8A8_UNorm` 相当、左上原点) のピクセルデータを出力し、任意のスレッドから呼び出せます:

```csharp
using (var decoded = Png.Decode(pngBytes))
{
    var width = decoded.Width;
    var height = decoded.Height;
    NativeArray<byte> rgba = decoded.AsNativeArray(); // ゼロコピーのビュー

    // メインスレッド上なら decoded.ToTexture2D() で Texture2D 化できます (反転込み)
}

// ピクセルをデコードせずヘッダ情報だけ読む
PngMeta pngMeta = Png.Info(pngBytes);     // Width / Height
WebPMeta webpMeta = WebP.Info(webpBytes); // Width / Height / HasAlpha / Format
```

fpng のデコーダーは fpng 自身がエンコードした PNG のみ読めます。`TryDecode` を使うと汎用デコーダーへ自然にフォールバックできます:

```csharp
if (!FPng.TryDecode(bytes, out var decoded))
{
    decoded = Png.Decode(bytes); // fpng 出力ではない → 汎用デコーダーで読む
}
using (decoded)
{
    // ...
}
```

## プロジェクト構造

```
.
├── FastImageConversion.Unity     # パッケージとテストをホストする Unity プロジェクト
│   ├── Packages
│   │   ├── FastImageConversion.Core   # 共通ヘルパー (PixelUtility、ハンドル基底)
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
