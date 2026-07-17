UNITY_PLUGIN_DIR_PNG  := ../FastImageConversion.Unity/Packages/FastImageConversion.Png/Runtime
UNITY_PLUGIN_DIR_FPNG := ../FastImageConversion.Unity/Packages/FastImageConversion.FPng/Runtime
UNITY_PLUGIN_DIR_WEBP := ../FastImageConversion.Unity/Packages/FastImageConversion.Webp/Runtime

.PHONY: all
all: linux-x64 windows-x64 macos-arm64 ios-arm64 android-arm64

.PHONY: build
build: build-linux-x64 build-windows-x64 build-macos-arm64 build-ios-arm64 build-android-arm64

.PHONY: install
install: install-linux-x64 install-windows-x64 install-macos-arm64 install-ios-arm64 install-android-arm64

.PHONY: linux-x64
linux-x64: build-linux-x64 install-linux-x64

.PHONY: windows-x64
windows-x64: build-windows-x64 install-windows-x64

.PHONY: macos-arm64
macos-arm64: build-macos-arm64 install-macos-arm64

.PHONY: ios-arm64
ios-arm64: build-ios-arm64 install-ios-arm64

.PHONY: android-arm64
android-arm64: build-android-arm64 install-android-arm64

.PHONY: build-linux-x64
build-linux-x64:
	@echo "Building for Linux (x64)..."
	rustup target add x86_64-unknown-linux-gnu
	cargo build --target x86_64-unknown-linux-gnu --profile release

.PHONY: build-macos-arm64
build-macos-arm64:
	@echo "Building for macOS (apple silicon)..."
	rustup target add aarch64-apple-darwin
	cargo build --target aarch64-apple-darwin --profile release

.PHONY: build-ios-arm64
build-ios-arm64:
	@echo "Building for iOS (aarch64)..."
	rustup target add aarch64-apple-ios
	IPHONEOS_DEPLOYMENT_TARGET=13 cargo build --target aarch64-apple-ios --profile release

.PHONY: build-android-arm64
build-android-arm64:
	@echo "Building for Android (arm64-v8a)..."
	rustup target add aarch64-linux-android
	cargo install cargo-ndk
    # libpthread に依存している関係で、fpngはAndroidでサポートできていない
	cargo ndk -t arm64-v8a build -p fic_png -p fic_webp --profile release

.PHONY: build-windows-x64
build-windows-x64:
	@echo "Building for Windows (x86_64)..."
	rustup target add x86_64-pc-windows-gnu
	cargo build --target x86_64-pc-windows-gnu --profile release

.PHONY: install-linux-x64
install-linux-x64:
	@echo "Installing Linux (x64) plugins..."
	mkdir -p $(UNITY_PLUGIN_DIR_PNG)/linux-x64 $(UNITY_PLUGIN_DIR_WEBP)/linux-x64 $(UNITY_PLUGIN_DIR_FPNG)/linux-x64
	cp target/x86_64-unknown-linux-gnu/release/libfic_png.so $(UNITY_PLUGIN_DIR_PNG)/linux-x64/
	cp target/x86_64-unknown-linux-gnu/release/libfic_webp.so $(UNITY_PLUGIN_DIR_WEBP)/linux-x64/
	cp target/x86_64-unknown-linux-gnu/release/libfic_fpng.so $(UNITY_PLUGIN_DIR_FPNG)/linux-x64/

.PHONY: install-macos-arm64
install-macos-arm64:
	@echo "Installing macOS (arm64) plugins..."
	mkdir -p $(UNITY_PLUGIN_DIR_PNG)/macOS-arm64 $(UNITY_PLUGIN_DIR_WEBP)/macOS-arm64 $(UNITY_PLUGIN_DIR_FPNG)/macOS-arm64
	cp target/aarch64-apple-darwin/release/libfic_png.dylib $(UNITY_PLUGIN_DIR_PNG)/macOS-arm64/
	cp target/aarch64-apple-darwin/release/libfic_webp.dylib $(UNITY_PLUGIN_DIR_WEBP)/macOS-arm64/
	cp target/aarch64-apple-darwin/release/libfic_fpng.dylib $(UNITY_PLUGIN_DIR_FPNG)/macOS-arm64/

.PHONY: install-ios-arm64
install-ios-arm64:
	@echo "Installing iOS (arm64) plugins..."
	mkdir -p $(UNITY_PLUGIN_DIR_PNG)/ios-arm64 $(UNITY_PLUGIN_DIR_WEBP)/ios-arm64 $(UNITY_PLUGIN_DIR_FPNG)/ios-arm64
	cp target/aarch64-apple-ios/release/libfic_png.a $(UNITY_PLUGIN_DIR_PNG)/ios-arm64/
	cp target/aarch64-apple-ios/release/libfic_webp.a $(UNITY_PLUGIN_DIR_WEBP)/ios-arm64/
	cp target/aarch64-apple-ios/release/libfic_fpng.a $(UNITY_PLUGIN_DIR_FPNG)/ios-arm64/

.PHONY: install-android-arm64
install-android-arm64:
	@echo "Installing Android (arm64) plugins..."
	mkdir -p $(UNITY_PLUGIN_DIR_PNG)/android-arm64 $(UNITY_PLUGIN_DIR_WEBP)/android-arm64
	cp target/aarch64-linux-android/release/libfic_png.so $(UNITY_PLUGIN_DIR_PNG)/android-arm64/
	cp target/aarch64-linux-android/release/libfic_webp.so $(UNITY_PLUGIN_DIR_WEBP)/android-arm64/

.PHONY: install-windows-x64
install-windows-x64:
	@echo "Installing Windows (x64) plugins..."
	mkdir -p $(UNITY_PLUGIN_DIR_PNG)/windows-x64 $(UNITY_PLUGIN_DIR_WEBP)/windows-x64 $(UNITY_PLUGIN_DIR_FPNG)/windows-x64
	cp target/x86_64-pc-windows-gnu/release/fic_png.dll $(UNITY_PLUGIN_DIR_PNG)/windows-x64/libfic_png.dll
	cp target/x86_64-pc-windows-gnu/release/fic_webp.dll $(UNITY_PLUGIN_DIR_WEBP)/windows-x64/libfic_webp.dll
	cp target/x86_64-pc-windows-gnu/release/fic_fpng.dll $(UNITY_PLUGIN_DIR_FPNG)/windows-x64/libfic_fpng.dll
