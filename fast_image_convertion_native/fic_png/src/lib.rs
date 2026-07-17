mod byte_buffer;

use std::ffi::{c_char, CString};
use std::io::Cursor;
use std::panic::{catch_unwind, AssertUnwindSafe};
use std::slice;
use png::{ColorType, Filter, Transformations};

#[repr(C)]
pub struct ByteBuffer {
    pub ptr: *mut u8,
    pub length: i32,
    pub capacity: i32,
}

#[repr(C)]
pub struct PngEncodingResult {
    pub success: bool,
    pub output: ByteBuffer,
    pub error_message: *const c_char,
}

impl PngEncodingResult {
    fn success(output: ByteBuffer) -> Self {
        Self {
            success: true,
            output,
            error_message: std::ptr::null(),
        }
    }

    fn error(err: png::EncodingError) -> Self {
        Self::error_msg(&err.to_string())
    }

    fn error_msg(msg: &str) -> Self {
        Self {
            success: false,
            output: ByteBuffer { ptr: std::ptr::null_mut(), length: 0, capacity: 0 },
            error_message: new_error_message(msg),
        }
    }
}

#[repr(C)]
pub struct PngDecodingResult {
    pub success: bool,
    /// RGBA8 (R,G,B,A の順で1チャンネル1バイト)
    pub output: ByteBuffer,
    pub width: u32,
    pub height: u32,
    pub error_message: *const c_char,
}

impl PngDecodingResult {
    fn success(output: ByteBuffer, width: u32, height: u32) -> Self {
        Self {
            success: true,
            output,
            width,
            height,
            error_message: std::ptr::null(),
        }
    }

    fn error_msg(msg: &str) -> Self {
        Self {
            success: false,
            output: ByteBuffer { ptr: std::ptr::null_mut(), length: 0, capacity: 0 },
            width: 0,
            height: 0,
            error_message: new_error_message(msg),
        }
    }
}

fn new_error_message(msg: &str) -> *const c_char {
    let error_message = CString::new(msg)
        .unwrap_or_else(|_| CString::new("Unknown error").unwrap());
    error_message.into_raw()
}

unsafe fn free_error_message(error_message: *const c_char) {
    if !error_message.is_null() {
        unsafe { let _ = CString::from_raw(error_message as *mut c_char); }
    }
}

// NOTE: pub extern "C" 関数は C#/Dart から直接呼ばれる FFI 境界のため、
// panic を境界の外へ unwind させてはならない。catch_unwind で握りつぶしてエラーを返す。

/// Only for GraphicsFormat.R8G8B8A8_UNorm
#[unsafe(no_mangle)]
pub extern "C" fn fic_png_encode(
    src_ptr: *const u8,
    src_length: i32,
    width: u32,
    height: u32,
) -> PngEncodingResult {
    catch_unwind(AssertUnwindSafe(|| {
        if src_ptr.is_null() || src_length <= 0 {
            return PngEncodingResult::error_msg("src_ptr is null or src_length is invalid");
        }

        let src_slice = unsafe {
            slice::from_raw_parts(src_ptr, src_length as usize)
        };

        match encode_png_internal(src_slice, width, height) {
            Ok(dest) => PngEncodingResult::success(ByteBuffer::from_vec(dest)),
            Err(e) => PngEncodingResult::error(e),
        }
    })).unwrap_or_else(|_| PngEncodingResult::error_msg("panic in fic_png_encode"))
}

fn encode_png_internal(src: &[u8], width: u32, height: u32) -> Result<Vec<u8>, png::EncodingError> {
    let mut dest = Vec::with_capacity(src.len() / 20);

    let info = png::Info::with_size(width, height);
    let mut encoder = png::Encoder::with_info(&mut dest, info)?;
    encoder.set_color(png::ColorType::Rgba);
    encoder.set_depth(png::BitDepth::Eight);
    encoder.set_compression(png::Compression::Fastest);
    encoder.set_filter(Filter::Adaptive); // 各行ごとに最適なフィルタを自動選択

    let mut writer = encoder.write_header()?;
    writer.write_image_data(src)?;
    writer.finish()?;

    Ok(dest)
}

/// 任意のPNGを RGBA8 にデコードする
#[unsafe(no_mangle)]
pub extern "C" fn fic_png_decode(
    src_ptr: *const u8,
    src_length: i32,
) -> PngDecodingResult {
    catch_unwind(AssertUnwindSafe(|| {
        if src_ptr.is_null() || src_length <= 0 {
            return PngDecodingResult::error_msg("src_ptr is null or src_length is invalid");
        }

        let src_slice = unsafe {
            slice::from_raw_parts(src_ptr, src_length as usize)
        };

        match decode_png_internal(src_slice) {
            Ok((rgba, width, height)) => {
                PngDecodingResult::success(ByteBuffer::from_vec(rgba), width, height)
            }
            Err(e) => PngDecodingResult::error_msg(&e.to_string()),
        }
    })).unwrap_or_else(|_| PngDecodingResult::error_msg("panic in fic_png_decode"))
}

fn decode_png_internal(src: &[u8]) -> Result<(Vec<u8>, u32, u32), png::DecodingError> {
    let mut decoder = png::Decoder::new(Cursor::new(src));
    // パレット/16bit深度/グレースケールを 8bit + アルファ付きへ正規化する
    decoder.set_transformations(Transformations::normalize_to_color8() | Transformations::ALPHA);

    let mut reader = decoder.read_info()?;
    let mut buf = vec![0u8; reader.output_buffer_size().unwrap_or_default()];
    let info = reader.next_frame(&mut buf)?;
    buf.truncate(info.buffer_size());

    let (width, height) = (info.width, info.height);

    let rgba = match info.color_type {
        ColorType::Rgba => buf,
        ColorType::GrayscaleAlpha => {
            // GA8 -> RGBA8
            let mut rgba = Vec::with_capacity(buf.len() * 2);
            for ga in buf.chunks_exact(2) {
                rgba.extend_from_slice(&[ga[0], ga[0], ga[0], ga[1]]);
            }
            rgba
        }
        // Transformations::ALPHA を指定しているため通常ここには来ないが、念のため変換する
        ColorType::Rgb => {
            let mut rgba = Vec::with_capacity(buf.len() / 3 * 4);
            for rgb in buf.chunks_exact(3) {
                rgba.extend_from_slice(&[rgb[0], rgb[1], rgb[2], 255]);
            }
            rgba
        }
        ColorType::Grayscale => {
            let mut rgba = Vec::with_capacity(buf.len() * 4);
            for g in buf {
                rgba.extend_from_slice(&[g, g, g, 255]);
            }
            rgba
        }
        ColorType::Indexed => {
            return Err(png::DecodingError::LimitsExceeded); // normalize済みのため到達しない
        }
    };

    Ok((rgba, width, height))
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_png_dispose(result: PngEncodingResult)  {
    let _ = catch_unwind(AssertUnwindSafe(|| {
        unsafe { free_error_message(result.error_message); }
        result.output.destroy();
    }));
}

#[unsafe(no_mangle)]
pub extern "C" fn fic_png_decode_dispose(result: PngDecodingResult)  {
    let _ = catch_unwind(AssertUnwindSafe(|| {
        unsafe { free_error_message(result.error_message); }
        result.output.destroy();
    }));
}

#[cfg(test)]
mod tests {
    use super::*;

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
        let (width, height) = (32u32, 16u32);
        let src = checker_rgba(width as usize, height as usize);

        let encoded = fic_png_encode(src.as_ptr(), src.len() as i32, width, height);
        assert!(encoded.success);
        assert!(encoded.output.length > 0);

        let decoded = fic_png_decode(encoded.output.ptr, encoded.output.length);
        assert!(decoded.success);
        assert_eq!(decoded.width, width);
        assert_eq!(decoded.height, height);

        let decoded_slice = unsafe {
            slice::from_raw_parts(decoded.output.ptr, decoded.output.length as usize)
        };
        assert_eq!(decoded_slice, &src[..]);

        fic_png_dispose(encoded);
        fic_png_decode_dispose(decoded);
    }

    #[test]
    fn test_decode_invalid_data() {
        let junk = [0u8; 16];
        let result = fic_png_decode(junk.as_ptr(), junk.len() as i32);
        assert!(!result.success);
        assert!(!result.error_message.is_null());
        fic_png_decode_dispose(result);
    }

    #[test]
    fn test_encode_invalid_args() {
        let result = fic_png_encode(std::ptr::null(), 0, 0, 0);
        assert!(!result.success);
        fic_png_dispose(result);
    }
}
