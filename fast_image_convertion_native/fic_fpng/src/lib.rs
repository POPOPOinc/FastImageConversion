pub mod wrapper;

#[allow(unused)]
use ::std::os::raw::*;
use std::panic::{catch_unwind, AssertUnwindSafe};

// fpng::FPNG_DECODE_* に対応するエラーコード (wrapper.h 参照)
const FPNG_DECODE_INVALID_ARG: i32 = 2;

// NOTE: pub extern "C" 関数は C# から直接呼ばれる FFI 境界のため、
// panic を境界の外へ unwind させてはならない。catch_unwind で握りつぶしてエラーを返す。
// (C++例外は wrapper.cpp 側で catch 済み)

#[unsafe(no_mangle)]
pub extern "C" fn fpng_init()
{
    let _ = catch_unwind(AssertUnwindSafe(|| {
        unsafe { wrapper::fpng_init_wrapper() }
    }));
}

#[unsafe(no_mangle)]
pub extern "C" fn fpng_cpu_supports_sse41() -> bool
{
    catch_unwind(AssertUnwindSafe(|| {
        unsafe { wrapper::fpng_cpu_supports_sse41_wrapper() }
    })).unwrap_or(false)
}

#[unsafe(no_mangle)]
pub extern "C" fn fpng_encode_image_to_memory(
    image: *const c_void,
    width: u32,
    height: u32,
    num_chans: u32,
    out_data: *mut *mut u8,
    out_size: *mut usize,
    out_context: *mut *mut c_void,
    flags: u32
) -> bool
{
    catch_unwind(AssertUnwindSafe(|| {
        unsafe {
            wrapper::fpng_encode_image_to_memory_wrapper(
                image,
                width,
                height,
                num_chans,
                out_data,
                out_size,
                out_context,
                flags
            )
        }
    })).unwrap_or(false)
}

/// fpngが出力したPNGのみデコード可能。
/// 戻り値はfpng::FPNG_DECODE_* のエラーコード (0 = success)。
/// 1 (FPNG_DECODE_NOT_FPNG) が返った場合は汎用PNGデコーダーへフォールバックすること。
#[unsafe(no_mangle)]
pub extern "C" fn fpng_decode_memory(
    image: *const c_void,
    image_size: u32,
    out_data: *mut *mut u8,
    out_size: *mut usize,
    out_width: *mut u32,
    out_height: *mut u32,
    out_channels_in_file: *mut u32,
    desired_channels: u32,
    out_context: *mut *mut c_void,
) -> i32
{
    catch_unwind(AssertUnwindSafe(|| {
        unsafe {
            wrapper::fpng_decode_memory_wrapper(
                image,
                image_size,
                out_data,
                out_size,
                out_width,
                out_height,
                out_channels_in_file,
                desired_channels,
                out_context
            )
        }
    })).unwrap_or(FPNG_DECODE_INVALID_ARG)
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn fpng_free(context: *mut c_void)
{
    let _ = catch_unwind(AssertUnwindSafe(|| {
        unsafe { wrapper::fpng_free_wrapper(context) }
    }));
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::ptr;
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
        fpng_init();

        let (width, height) = (32u32, 16u32);
        let src = checker_rgba(width as usize, height as usize);

        let mut enc_data: *mut u8 = ptr::null_mut();
        let mut enc_size: usize = 0;
        let mut enc_context: *mut c_void = ptr::null_mut();
        let ok = fpng_encode_image_to_memory(
            src.as_ptr() as *const c_void, width, height, 4,
            &mut enc_data, &mut enc_size, &mut enc_context, 0,
        );
        assert!(ok);
        assert!(enc_size > 0);

        let mut dec_data: *mut u8 = ptr::null_mut();
        let mut dec_size: usize = 0;
        let mut dec_width = 0u32;
        let mut dec_height = 0u32;
        let mut dec_channels = 0u32;
        let mut dec_context: *mut c_void = ptr::null_mut();
        let result = fpng_decode_memory(
            enc_data as *const c_void, enc_size as u32,
            &mut dec_data, &mut dec_size,
            &mut dec_width, &mut dec_height, &mut dec_channels,
            4, &mut dec_context,
        );
        assert_eq!(result, 0);
        assert_eq!(dec_width, width);
        assert_eq!(dec_height, height);

        let decoded = unsafe { slice::from_raw_parts(dec_data, dec_size) };
        assert_eq!(decoded, &src[..]);

        unsafe {
            fpng_free(enc_context);
            fpng_free(dec_context);
        }
    }

    #[test]
    fn test_decode_not_fpng() {
        fpng_init();

        // fpng以外のエンコーダーが出力したPNG(っぽいデータ)は NOT_FPNG またはエラーになる
        let junk = [0x89u8, 0x50, 0x4E, 0x47, 0, 0, 0, 0, 0, 0, 0, 0];
        let mut dec_data: *mut u8 = ptr::null_mut();
        let mut dec_size: usize = 0;
        let mut dec_width = 0u32;
        let mut dec_height = 0u32;
        let mut dec_channels = 0u32;
        let mut dec_context: *mut c_void = ptr::null_mut();
        let result = fpng_decode_memory(
            junk.as_ptr() as *const c_void, junk.len() as u32,
            &mut dec_data, &mut dec_size,
            &mut dec_width, &mut dec_height, &mut dec_channels,
            4, &mut dec_context,
        );
        assert_ne!(result, 0);
        assert!(dec_context.is_null());
    }
}
