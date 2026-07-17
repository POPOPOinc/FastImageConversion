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

// fpng::FPNG_DECODE_* のエラーコードを返す (0 = success)
// fpngが出力したPNGのみデコード可能。FPNG_DECODE_NOT_FPNG(=1)が返った場合は
// 汎用PNGデコーダー(fic_png)へフォールバックすること
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
);

void fpng_free_wrapper(void* context);

#ifdef __cplusplus
}
#endif

#endif
