use std::mem::MaybeUninit;
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::ptr;
use libwebp_sys::{WebPConfig, WebPEncode, WebPFree, WebPMemoryWrite, WebPMemoryWriter, WebPMemoryWriterClear, WebPMemoryWriterInit, WebPPicture, WebPPictureFree, WebPPictureImportRGBA, WebPValidateConfig, WebPBitstreamFeatures, WebPDecode, WebPDecoderConfig, WebPDecoderOptions, WebPFreeDecBuffer, WebPGetFeatures,VP8StatusCode, WEBP_CSP_MODE, WebPDecBuffer};
pub use libwebp_sys::WebPPreset;

pub use libwebp_sys::WebPEncodingError;

#[repr(C)]
#[derive(Debug)]
pub enum WebpEncodingErrorCode {
    None = 0,
    OutOfMemory = 1,
    BitstreamOutOfMemory = 2,
    NullParameter = 3,
    InvalidConfiguration = 4,
    BadDimension = 5,
    Partition0Overflow = 6,
    PartitionOverflow = 7,
    BadWrite = 8,
    FileTooBig = 9,
    UserAbort = 10,
    Last = 11,
}

impl From<WebPEncodingError> for WebpEncodingErrorCode {
    fn from(value: WebPEncodingError) -> Self {
        match value {
            WebPEncodingError::VP8_ENC_OK => WebpEncodingErrorCode::None,
            WebPEncodingError::VP8_ENC_ERROR_OUT_OF_MEMORY => WebpEncodingErrorCode::OutOfMemory,
            WebPEncodingError::VP8_ENC_ERROR_BITSTREAM_OUT_OF_MEMORY => WebpEncodingErrorCode::BitstreamOutOfMemory,
            WebPEncodingError::VP8_ENC_ERROR_NULL_PARAMETER => WebpEncodingErrorCode::NullParameter,
            WebPEncodingError::VP8_ENC_ERROR_INVALID_CONFIGURATION => WebpEncodingErrorCode::InvalidConfiguration,
            WebPEncodingError::VP8_ENC_ERROR_BAD_DIMENSION => WebpEncodingErrorCode::BadDimension,
            WebPEncodingError::VP8_ENC_ERROR_PARTITION0_OVERFLOW => WebpEncodingErrorCode::Partition0Overflow,
            WebPEncodingError::VP8_ENC_ERROR_PARTITION_OVERFLOW => WebpEncodingErrorCode::PartitionOverflow,
            WebPEncodingError::VP8_ENC_ERROR_BAD_WRITE => WebpEncodingErrorCode::BadWrite,
            WebPEncodingError::VP8_ENC_ERROR_FILE_TOO_BIG => WebpEncodingErrorCode::FileTooBig,
            WebPEncodingError::VP8_ENC_ERROR_USER_ABORT => WebpEncodingErrorCode::UserAbort,
            WebPEncodingError::VP8_ENC_ERROR_LAST => WebpEncodingErrorCode::Last,
        }
    }
}


#[repr(C)]
#[derive(Debug)]
pub enum WebpDecodingErrorCode {
    None = 0,
    OutOfMemory = 1,
    InvalidParam = 2,
    BitstreamError = 3,
    UnsupportedFeature = 4,
    Suspended = 5,
    UserAbort = 6,
    NotEnoughData = 7,
}

impl From<VP8StatusCode> for WebpDecodingErrorCode {
    fn from(value: VP8StatusCode) -> Self {
        match value {
            VP8StatusCode::VP8_STATUS_OK => WebpDecodingErrorCode::None,
            VP8StatusCode::VP8_STATUS_OUT_OF_MEMORY => WebpDecodingErrorCode::OutOfMemory,
            VP8StatusCode::VP8_STATUS_INVALID_PARAM => WebpDecodingErrorCode::InvalidParam,
            VP8StatusCode::VP8_STATUS_BITSTREAM_ERROR => WebpDecodingErrorCode::BitstreamError,
            VP8StatusCode::VP8_STATUS_UNSUPPORTED_FEATURE => WebpDecodingErrorCode::UnsupportedFeature,
            VP8StatusCode::VP8_STATUS_SUSPENDED => WebpDecodingErrorCode::Suspended,
            VP8StatusCode::VP8_STATUS_USER_ABORT => WebpDecodingErrorCode::UserAbort,
            VP8StatusCode::VP8_STATUS_NOT_ENOUGH_DATA => WebpDecodingErrorCode::NotEnoughData,
        }
    }
}

#[repr(C)]
pub struct WebpEncodingResult {
    pub success: bool,
    pub output: ByteBuffer,
    pub error_code: WebpEncodingErrorCode,
}

impl WebpEncodingResult {
    pub(crate) fn error(error_code: WebpEncodingErrorCode) -> Self {
        WebpEncodingResult {
            success: false,
            output: ByteBuffer::empty(),
            error_code,
        }
    }
}

#[repr(C)]
pub struct WebpDecodingResult {
    pub output: WebPDecBuffer,
    pub meta: WebPBitstreamFeatures,
    pub error_code: WebpDecodingErrorCode,
}

impl WebpDecodingResult {
    fn error(error_code: WebpDecodingErrorCode) -> Self {
        WebpDecodingResult {
            output: unsafe { std::mem::zeroed() },
            meta: unsafe { std::mem::zeroed() },
            error_code,
        }
    }
}

#[repr(C)]
pub struct WebpInfoResult {
    pub meta: WebPBitstreamFeatures,
    pub error_code: WebpDecodingErrorCode,
}

#[repr(C)]
pub struct ByteBuffer {
    pub ptr: *mut u8,
    pub length: i32,
}

impl ByteBuffer {
    pub fn empty() -> Self {
        ByteBuffer {
            ptr: ptr::null_mut(),
            length: 0,
        }
    }
}

// NOTE: pub extern "C" 関数は C# から直接呼ばれる FFI 境界のため、
// panic を境界の外へ unwind させてはならない。catch_unwind で握りつぶしてエラーを返す。

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_new_config(preset: WebPPreset, quality_factor: f32) -> WebPConfig {
    catch_unwind(AssertUnwindSafe(|| {
        match WebPConfig::new_with_preset(preset, quality_factor) {
            Ok(config) => config,
            Err(_) => unsafe { std::mem::zeroed() },
        }
    })).unwrap_or_else(|_| unsafe { std::mem::zeroed() })
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_encode(
    src_ptr: *mut u8,
    src_length: i32,
    width: i32,
    height: i32,
    config: WebPConfig,
) -> WebpEncodingResult {
    catch_unwind(AssertUnwindSafe(|| {
        // RGBA (4バイト/ピクセル) 前提の入力サイズチェック
        if src_ptr.is_null() || width <= 0 || height <= 0 || (src_length as i64) < (width as i64) * (height as i64) * 4 {
            return WebpEncodingResult::error(WebpEncodingErrorCode::NullParameter);
        }

        let Ok(mut picture) = WebPPicture::new() else {
            return WebpEncodingResult::error(WebpEncodingErrorCode::InvalidConfiguration);
        };

        if unsafe { WebPValidateConfig(&config) as i32 } != 1 {
            return WebpEncodingResult::error(WebpEncodingErrorCode::InvalidConfiguration);
        }

        let stride = width * 4; // [r,g,b,a]
        picture.width = width;
        picture.height = height;
        // lossless の場合は YUV ではなく ARGB バッファへインポートする必要がある
        picture.use_argb = if config.lossless != 0 { 1 } else { 0 };

        let mut writer: MaybeUninit<WebPMemoryWriter> = core::mem::MaybeUninit::uninit();

        let ok = unsafe {
            WebPMemoryWriterInit(writer.as_mut_ptr());
            picture.writer = Some(WebPMemoryWrite);
            picture.custom_ptr = writer.as_mut_ptr() as *mut ::core::ffi::c_void;
            WebPPictureImportRGBA(&mut picture, src_ptr, stride);
            WebPEncode(&config, &mut picture)
        };


        // error
        if ok == 0 {
            let error_code: WebpEncodingErrorCode = picture.error_code.into();

            unsafe {
                WebPMemoryWriterClear(writer.as_mut_ptr());
                WebPPictureFree(&mut picture);
            }

            return WebpEncodingResult {
                success: false,
                error_code,
                output: ByteBuffer::empty(),
            }
        }

        unsafe {
            WebPPictureFree(&mut picture);
        }

        let writer = unsafe {
            writer.assume_init_read()
        };

        let output = ByteBuffer {
            ptr: writer.mem,
            length: writer.size as i32,
        };

        WebpEncodingResult {
            success: true,
            output,
            error_code: WebpEncodingErrorCode::None
        }
    })).unwrap_or_else(|_| WebpEncodingResult::error(WebpEncodingErrorCode::InvalidConfiguration))
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_new_decoder_options() -> WebPDecoderOptions {
    catch_unwind(AssertUnwindSafe(|| {
        match WebPDecoderConfig::new() {
            Ok(config) => config.options,
            Err(_) => unsafe { std::mem::zeroed() },
        }
    })).unwrap_or_else(|_| unsafe { std::mem::zeroed() })
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_info(
    data_ptr: *const u8,
    data_length: i32,
) -> WebpInfoResult {
    catch_unwind(AssertUnwindSafe(|| {
        let mut features = MaybeUninit::<WebPBitstreamFeatures>::uninit();
        let status = unsafe {
            WebPGetFeatures(data_ptr, data_length as usize, features.as_mut_ptr())
        };
        if status != VP8StatusCode::VP8_STATUS_OK {
            return WebpInfoResult {
                meta: WebPBitstreamFeatures {
                    width: 0,
                    height: 0,
                    has_alpha: 0,
                    has_animation: 0,
                    format: 0,
                    pad: [0,0,0,0,0],
                },
                error_code: status.into(),
            };
        }
        let features = unsafe { features.assume_init() };
        WebpInfoResult {
            meta: features,
            error_code: WebpDecodingErrorCode::None,
        }
    })).unwrap_or_else(|_| WebpInfoResult { meta: unsafe { std::mem::zeroed() }, error_code: WebpDecodingErrorCode::InvalidParam })
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_decode(
    data_ptr: *const u8,
    data_length: i32,
    options: WebPDecoderOptions,
) -> WebpDecodingResult {
    catch_unwind(AssertUnwindSafe(|| {
        let Ok(mut config) = WebPDecoderConfig::new() else {
            return WebpDecodingResult::error(WebpDecodingErrorCode::InvalidParam);
        };

        config.options = options;
        config.output.colorspace = WEBP_CSP_MODE::MODE_RGBA;

        let status = unsafe {
            WebPDecode(data_ptr, data_length as usize, &mut config)
        };

        if status != VP8StatusCode::VP8_STATUS_OK {
            unsafe { WebPFreeDecBuffer(&mut config.output) };
            return WebpDecodingResult::error(status.into());
        }

        WebpDecodingResult {
            output: config.output,
            meta: config.input,
            error_code: WebpDecodingErrorCode::None,
        }
    })).unwrap_or_else(|_| WebpDecodingResult::error(WebpDecodingErrorCode::InvalidParam))
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_dispose(result: WebpEncodingResult) {
    catch_unwind(AssertUnwindSafe(|| {
        unsafe {
            WebPFree(result.output.ptr as *mut std::ffi::c_void);
        }
    })).unwrap_or_else(|_| ())
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_webp_decode_dispose(mut result: WebpDecodingResult) {
    catch_unwind(AssertUnwindSafe(|| {
        unsafe {
            WebPFreeDecBuffer(&mut result.output);
        }
    })).unwrap_or_else(|_| ())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::slice;

    fn checker_rgba(width: usize, height: usize) -> Vec<u8> {
        let mut src = Vec::with_capacity(width * height * 4);
        for y in 0..height {
            for x in 0..width {
                let v = if (x + y) % 2 == 0 { 255 } else { 0 };
                src.extend_from_slice(&[v, 0, 255 - v, 255]);
            }
        }
        src
    }

    #[test]
    fn test_encode_decode_roundtrip() {
        let (width, height) = (32i32, 16i32);
        let mut src = checker_rgba(width as usize, height as usize);

        // ロスレス設定でエンコードして完全一致を検証する
        let mut config = fic_webp_new_config(WebPPreset::WEBP_PRESET_DEFAULT, 100.0);
        config.lossless = 1;
        config.exact = 1;

        let encoded = fic_webp_encode(src.as_mut_ptr(), src.len() as i32, width, height, config);
        assert!(encoded.success);
        assert!(encoded.output.length > 0);

        let info = fic_webp_info(encoded.output.ptr, encoded.output.length);
        assert!(matches!(info.error_code, WebpDecodingErrorCode::None));
        assert_eq!(info.meta.width, width);
        assert_eq!(info.meta.height, height);

        let options = fic_webp_new_decoder_options();
        let decoded = fic_webp_decode(encoded.output.ptr, encoded.output.length, options);
        assert!(matches!(decoded.error_code, WebpDecodingErrorCode::None));

        let rgba = unsafe { decoded.output.u.RGBA };
        let decoded_slice = unsafe { slice::from_raw_parts(rgba.rgba, rgba.size) };
        assert_eq!(decoded_slice, &src[..]);

        fic_webp_dispose(encoded);
        fic_webp_decode_dispose(decoded);
    }

    #[test]
    fn test_encode_invalid_args() {
        let config = fic_webp_new_config(WebPPreset::WEBP_PRESET_DEFAULT, 50.0);
        let result = fic_webp_encode(std::ptr::null_mut(), 0, 0, 0, config);
        assert!(!result.success);
    }

    #[test]
    fn test_decode_invalid_data() {
        let junk = [0u8; 16];
        let options = fic_webp_new_decoder_options();
        let decoded = fic_webp_decode(junk.as_ptr(), junk.len() as i32, options);
        assert!(!matches!(decoded.error_code, WebpDecodingErrorCode::None));
    }
}
