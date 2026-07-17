use std::fs::File;
use std::io::{Read, Write};
use std::{ptr, slice};
use std::ffi::{c_char, c_void, CStr, CString};
use std::path::PathBuf;
use clap::{Parser, ValueEnum};
use fic_fpng::{fpng_encode_image_to_memory, fpng_free, fpng_init};
use fic_webp::{fic_webp_encode, fic_webp_dispose, fic_webp_new_config, WebPPreset};
use fic_png::{fic_png_dispose, fic_png_encode};

#[derive(Debug, Clone, Copy, ValueEnum)]
#[clap(rename_all = "lower")]
enum PngEncoder {
    Png,
    #[clap(name = "fpng")]
    FPng,
}

#[derive(Parser, Debug)]
#[command(name = "fic")]
#[command(about = "Convert input pixel data (RGBARGBA...) to .png or .webp")]
struct Args {
    input: PathBuf,
    output: PathBuf,
    #[arg(long)]
    width: u32,
    #[arg(long)]
    height: u32,
    #[arg(long, default_value = "50.0")]
    quality: f32,
    #[arg(long, value_enum, default_value = "fpng")]
    png_encoder: PngEncoder,
}

fn main() {
    let args = Args::parse();

    let mut file = File::open(&args.input).expect("Could not open input file");
    let mut buffer: Vec<u8> = Vec::new();
    file.read_to_end(&mut buffer).expect("Could not read input file");

    let ptr: *mut u8 = buffer.as_mut_ptr();

    match args.output.extension().and_then(|s| s.to_str()) {
        Some("png") => {
            match args.png_encoder {
                PngEncoder::Png => {
                    let result = fic_png_encode(ptr, buffer.len() as i32, args.width, args.height);
                    if !result.success {
                        let error_message = unsafe { CStr::from_ptr(result.error_message).to_string_lossy() };
                        eprintln!("Failed to encode PNG : {error_message}");
                        std::process::exit(1);
                    }

                    write_to_file(&args.output, result.output.ptr, result.output.length);
                    fic_png_dispose(result);
                }
                PngEncoder::FPng => {
                    fpng_init();

                    let mut out_data: *mut u8 = ptr::null_mut();
                    let mut out_size: usize = 0;
                    let mut out_context: *mut c_void = ptr::null_mut();

                    let result =
                        fpng_encode_image_to_memory(
                            ptr as *const c_void,
                            args.width,
                            args.height,
                            4,
                            &mut out_data,
                            &mut out_size,
                            &mut out_context,
                            0,
                        );

                    if !result {
                        eprintln!("Failed to encode PNG");
                        std::process::exit(1);
                    }

                    write_to_file(&args.output, out_data, out_size as i32);
                    unsafe {
                        fpng_free(out_context);
                    }
                }
            }
        }
        Some("webp") => {
            let config = fic_webp_new_config(WebPPreset::WEBP_PRESET_DEFAULT, args.quality);
            let result = fic_webp_encode(
                ptr,
                buffer.len() as i32,
                args.width as i32,
                args.height as i32, 
                config);
            
            if !result.success {
                eprintln!("Failed to encode WebP {:?}", result.error_code);
                std::process::exit(1);
            }
            write_to_file(&args.output, result.output.ptr, result.output.length);
            fic_webp_dispose(result);
        }
        _ => {
            eprintln!("Unsupported output format. Use .png or .webp extension.");
            std::process::exit(1);
        }
    }
}

fn write_to_file(path: &PathBuf, ptr: *const u8, length: i32) {
    let slice = unsafe {
        slice::from_raw_parts(ptr, length as usize)
    };
    let mut file = File::create(&path).expect("Could not create output file");
    file.write_all(slice).expect("Could not write output file");
}
