mod byte_buffer;

use std::ffi::{c_char, CString};
use std::slice;
use png::Filter;

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
        let error_message = CString::new(msg)
            .unwrap_or_else(|_| CString::new("Unknown error").unwrap());
        Self {
            success: false,
            output: ByteBuffer { ptr: std::ptr::null_mut(), length: 0, capacity: 0 },
            error_message: error_message.into_raw(),
        }
    }
}

/// Only for GraphicsFormat.R8G8B8A8_UNorm
#[unsafe(no_mangle)]
pub extern "C" fn fic_png_encode(
    src_ptr: *const u8,
    src_length: i32,
    width: u32,
    height: u32,
) -> PngEncodingResult {
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

#[unsafe(no_mangle)]
pub extern "C" fn fic_png_dispose(result: PngEncodingResult)  {
    if !result.error_message.is_null() {
        unsafe { let _ = CString::from_raw(result.error_message as *mut c_char) ; }
    }
    result.output.destroy();
}
