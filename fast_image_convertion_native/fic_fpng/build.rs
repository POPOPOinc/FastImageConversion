use std::env;
use std::error::Error;

fn main() -> Result<(), Box<dyn Error>> {
    let target = env::var("TARGET")?;

    let mut build = cc::Build::new();
    build
        .cpp(true)
        .file("vendor/fpng/src/fpng.cpp")
        .file("wrapper.cpp")
        .include("vendor/fpng/src")
        .opt_level(3)
        .flag_if_supported("-fno-strict-aliasing")
        .define("FPNG_NO_STDIO", "1");

    if target.contains("x86_64") {
        build
            .define("FPNG_NO_SSE", "0")
            .define("FPNG_X86_OR_X64_CPU", "1")
            .flag("-msse4.1")
            .flag("-mpclmul");
    }

    build.compile("fpng");

    bindgen::Builder::default()
        .header("wrapper.h")
        .clang_arg("-I.")
        .clang_arg("-Ifpng")
        .clang_arg("-x")
        .clang_arg("c++")
        .clang_arg("-std=c++11")
        .parse_callbacks(Box::new(bindgen::CargoCallbacks::new()))
        .allowlist_function("fpng_.*")
        .allowlist_var("FPNG_.*")
        .allowlist_type("fpng_.*")
        .clang_arg("-DFPNG_NO_STDIO=1")
        .generate()
        .expect("Unable to generate bindings")
        .write_to_file("src/wrapper.rs")?;

    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_namespace("FastImageConversion")
        .csharp_dll_name("libfic_fpng")
        .csharp_dll_name_if("UNITY_IOS && !UNITY_EDITOR", "__Internal")
        .csharp_entry_point_prefix("")
        .csharp_use_function_pointer(false)
        .generate_csharp_file("../../Packages/FastImageConversion.FPng/Runtime/NativeMethods.g.cs")?;

    println!("cargo:rerun-if-changed=wrapper.h");
    println!("cargo:rerun-if-changed=wrapper.cpp");
    println!("cargo:rerun-if-changed=fpng/fpng.h");
    println!("cargo:rerun-if-changed=fpng/fpng.cpp");

    if target.contains("linux") {
        println!("cargo:rustc-link-lib=stdc++");
        println!("cargo:rustc-link-lib=m");
        println!("cargo:rustc-link-lib=pthread");
    } else if target.contains("apple") {
        println!("cargo:rustc-link-lib=c++");
    }

    Ok(())
}
