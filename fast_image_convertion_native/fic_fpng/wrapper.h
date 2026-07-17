#ifndef FPNG_WRAPPER_H
#define FPNG_WRAPPER_H

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

void fpng_init_wrapper(void);
bool fpng_cpu_supports_sse41_wrapper(void);

bool fpng_encode_image_to_memory_wrapper(
    const void* image,
    uint32_t width,
    uint32_t height,
    uint32_t num_chans,
    uint8_t** out_data,
    size_t* out_size,
    void** out_context,
    uint32_t flags
);

void fpng_free_wrapper(void* context);

#ifdef __cplusplus
}
#endif

#endif