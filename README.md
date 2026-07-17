# FastImageConversion

Unity向け、画像エンコード処理を提供するプラグインです。

現在、以下のフォーマットに対応しています
- png
  - 二種類の実装から選択可能
      - rustの[image-rs/png](https://github.com/image-rs/image-png) 実装
      - c++の[fpng](https://github.com/richgel999/fpng) 実装
- webp
  - libwebpのエンコード速度に最適化したビルド
  
  
また、UnityEngine.ImageConversion と違って、全ての機能をメインスレッド以外から利用可能です。

## Features

|      | Encode | Decode |
|------|---------------|--------|
| Webp | :white_check_mark: | :white_check_mark: |
| PNG (fpng) | :white_check_mark: | :x: |
| PNG (image-rs) | :white_check_mark: | :x: |
| Jpeg | :x: | :x: |



## パフォーマンス

|      | latency(median) | size |
|------|---------------|--------|
| UnityEngine.ImageConversion (PNG) | 29.29ms | 87,348B |
| FastImageConversion (WEBP) | 16.26ms | 9,950B |
| FastImageConversion (PNG\|image-rs) | 4.31ms | 82,459B |
| FastImageConversion (PNG\|richgel999/fpng) | 3.04ms | 88,362B |

- SpaceThumbnailServer 出力の 360x280 テクスチャを入力にとった場合のサンプル
- apple m4 / Unityエディタ上


## プロジェクト構造

各ネイティブプラグインは、Rustのビルドツールチェインを使ってビルドされています。

`fast_image_conversion` ディレクトリにあるRustワークスペースから、各ネイティブプラグインのビルドが行えるようになっています。

- fic_fpng
  - c++製のfpngと、Unity用のC ABIラッパーを提供するプロジェクトです
- fic_png
  - image-rsに依存したpngエンコーダーと、Unity用のラッパーを提供するプロジェクトです
- fic_webp
  - libwebp-sys crateで libwebpをビルドして、Unity用のC ABIを提供するプロジェクトです
  

### ビルド手順

このプラグインは 現在、SpaceThumbnailServerからのみの利用を想定しているため、以下の環境のネイティブプラグインを作成しています

- Linux (x64)
- Windows (x64)
- macOS (arm64)

makeコマンドで、各ターゲットをcargoでクロスビルドし、Unityアセットとして配置します

```bash
# 一括で3つのターゲットをビルドする
$ make

# ターゲットを指定したビルド
$ make linux-x64
$ make windows-x64
$ make macos-arm64
```

#### ビルドが通らない場合

fpng のビルドには c++のクロスビルドが必要になりますが、rustupだけだとc++のクロスビルドツールがサポートされるわけではないので、
ビルドのためには実機が必要になるかもしれません。

- macOSで linux-x64 ビルドが通らない場合
  - build/ ディレクトリにあるdocker-compose.yaml で コンテナを立ち上げて、その中でビルドするとうまくいきます
  - ```bash
    $ docker compose run --rm rust-builder-x64 bash
    # make build-linux-x64
    # exit
    $ make install-linux-x64
    ```
- windowsからクロスビルドが通らない場合
  - TODO:

TODO: CI でネイティブプラグインを焼くアクションを用意したい


## Usage

ネイティブプラグイン側が入力にとる画像データは、4バイト1組のピクセルデータが連続して並んだものです。

```
RGBARGBARGBA..
```

- R,G,B,A 各チャンネルは 各1バイト
    - 1バイト0-255 が 0-1 を意味します
- 各種画像ライブラリに合わせて、左上が原点になっていることを期待します。

Unity の `GraphicsFormat.R8G8B8A8_UNorm` 形式とほぼ同一ですが、Unityのテクスチャは原点が **左下** なので、
ネイティブプラグインに渡す前に上下を反転させる必要があります。

### Unity plugins

TODO:

### cli tool

TODO:
