fn main() -> Result<(), Box<dyn std::error::Error>> {
    //let out_dir = std::path::PathBuf::from(std::env::var("OUT_DIR").unwrap());
    //tonic_build::configure()
    //    .build_server(true)
    //    .build_client(false)
    //    .file_descriptor_set_path(out_dir.join("compression_service_descriptor.bin"))
    //    .compile_protos(&["../proto/compression.proto"], &["../proto"])?;
    //
    prost_build::compile_protos(&["../proto/compression.proto"], &["../proto"])
        .expect("failed to compile proto");

    Ok(())
}
