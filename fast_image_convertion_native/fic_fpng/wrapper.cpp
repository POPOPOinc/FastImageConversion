// c++メソッドを c abi として露出させるためのラッパー
// NOTE: C++の例外をC ABI境界の外へ漏らさないよう、各関数で catch しておく
#include "vendor/fpng/src/fpng.h"
#include "wrapper.h"
#include <cstdlib>
#include <cstring>
#include <vector>

extern "C" {

void fpng_init_wrapper(void) {
    fpng::fpng_init();
}

bool fpng_cpu_supports_sse41_wrapper(void) {
    return fpng::fpng_cpu_supports_sse41();
}

bool fpng_encode_image_to_memory_wrapper(
    const void* image,
    uint32_t width,
    uint32_t height,
    uint32_t num_chans,
    uint8_t** out_data,
    size_t* out_size,
    void** out_context,
    uint32_t flags
) {
    if (!out_data || !out_size || !out_context) {
        return false;
    }

    std::vector<uint8_t>* vec_ptr = nullptr;
    try {
        vec_ptr = new std::vector<uint8_t>();

        bool success = fpng::fpng_encode_image_to_memory(
            image, width, height, num_chans, *vec_ptr, flags
        );

        if (success && !vec_ptr->empty()) {
            *out_size = vec_ptr->size();
            *out_data = vec_ptr->data();
            *out_context = static_cast<void*>(vec_ptr);
            return true;
        }
    } catch (...) {
        // bad_alloc など。下のクリーンアップへフォールスルー
    }

    delete vec_ptr;
    *out_data = nullptr;
    *out_size = 0;
    *out_context = nullptr;
    return false;
}

int32_t fpng_decode_memory_wrapper(
    const void* image,
    uint32_t image_size,
    uint8_t** out_data,
    size_t* out_size,
    uint32_t* out_width,
    uint32_t* out_height,
    uint32_t* out_channels_in_file,
    uint32_t desired_channels,
    void** out_context
) {
    if (!image || !out_data || !out_size || !out_width || !out_height || !out_channels_in_file || !out_context) {
        return fpng::FPNG_DECODE_INVALID_ARG;
    }

    *out_data = nullptr;
    *out_size = 0;
    *out_width = 0;
    *out_height = 0;
    *out_channels_in_file = 0;
    *out_context = nullptr;

    std::vector<uint8_t>* vec_ptr = nullptr;
    try {
        vec_ptr = new std::vector<uint8_t>();

        uint32_t width = 0, height = 0, channels_in_file = 0;
        int result = fpng::fpng_decode_memory(
            image, image_size, *vec_ptr, width, height, channels_in_file, desired_channels
        );

        if (result == fpng::FPNG_DECODE_SUCCESS && !vec_ptr->empty()) {
            *out_size = vec_ptr->size();
            *out_data = vec_ptr->data();
            *out_width = width;
            *out_height = height;
            *out_channels_in_file = channels_in_file;
            *out_context = static_cast<void*>(vec_ptr);
            return result;
        }

        delete vec_ptr;
        return result;
    } catch (...) {
        delete vec_ptr;
        return fpng::FPNG_DECODE_FAILED_CHUNK_PARSING;
    }
}

void fpng_free_wrapper(void* context) {
    if (context) {
        delete static_cast<std::vector<uint8_t>*>(context);
    }
}

}
