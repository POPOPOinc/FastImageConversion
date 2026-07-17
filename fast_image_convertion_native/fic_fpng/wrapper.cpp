// c++メソッドを c abi として露出させるためのラッパー
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

    std::vector<uint8_t>* vec_ptr = new std::vector<uint8_t>();

    bool success = fpng::fpng_encode_image_to_memory(
        image, width, height, num_chans, *vec_ptr, flags
    );

    if (success && !vec_ptr->empty()) {
        *out_size = vec_ptr->size();
        *out_data = vec_ptr->data();
        *out_context = static_cast<void*>(vec_ptr);
        return true;
    }

    delete vec_ptr;
    *out_data = nullptr;
    *out_size = 0;
    *out_context = nullptr;
    return false;
}

void fpng_free_wrapper(void* context) {
    if (context) {
        delete static_cast<std::vector<uint8_t>*>(context);
    }
}

}
