use std::error::Error;

fn main() -> Result<(), Box<dyn Error>> {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_namespace("FastImageConversion")
        .csharp_dll_name("libfic_png")
        .csharp_dll_name_if("UNITY_IOS && !UNITY_EDITOR", "__Internal")
        .csharp_use_function_pointer(false)
        .generate_csharp_file("../../Packages/FastImageConversion.Png/Runtime/NativeMethods.g.cs")?;

    Ok(())
}
