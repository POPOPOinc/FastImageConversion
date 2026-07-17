pub mod wrapper;

#[allow(unused)]
use ::std::os::raw::*;
use std::slice;

#[unsafe(no_mangle)]
pub extern "C" fn fpng_init()
{
    unsafe { wrapper::fpng_init_wrapper() }
}

#[unsafe(no_mangle)]
pub extern "C" fn fpng_cpu_supports_sse41() -> bool
{
    unsafe { wrapper::fpng_cpu_supports_sse41_wrapper() }
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
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn fpng_free(context: *mut c_void)
{
    unsafe { wrapper::fpng_free_wrapper(context) }
}
