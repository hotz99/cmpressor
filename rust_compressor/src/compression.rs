use std::io::Write;
use std::process::{Command, Stdio};

pub fn compress_video(
    video_data: &[u8],
    video_id: &str,
    output_format: &str,
    codec: &str,
) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    // mp4 requires seekable output, which stdout is not
    // hence we write to a tmpfs (in-memory filesystem) file
    // still requires syscall
    let output_file_path = format!("/tmp/{}.{}", video_id, output_format);

    let mut ffmpeg = Command::new("ffmpeg")
        .arg("-loglevel")
        .arg("error")
        .arg("-i")
        .arg("-")
        .arg("-c:v")
        .arg(codec)
        .arg("-f")
        .arg(output_format)
        .arg(output_file_path.as_str())
        .stdin(Stdio::piped())
        .spawn()?;

    let stdin = ffmpeg.stdin.as_mut().unwrap();

    stdin.write_all(video_data).map_err(|e| {
        format!(
            "failed to write video data to ffmpeg stdin: {}",
            e.to_string()
        )
    })?;

    if let Err(err) = ffmpeg.wait() {
        return Err(format!("ffmpeg failed to perform compression: {}", err).into());
    } else {
        let read_result = std::fs::read(&output_file_path)
            .map_err(|e| format!("failed to read file at: {}: {}", output_file_path, e))?;

        std::fs::remove_file(&output_file_path)
            .map_err(|e| format!("failed to remove file at: {}: {}", output_file_path, e))?;

        Ok(read_result)
    }
}

#[test]
fn test_compress_video() {
    let video_data = include_bytes!("../assets/test.mp4");
    let id = uuid::Uuid::new_v4().to_string();
    let codec = "libx264";
    let output_format = "mp4";
    let compressed_video = compress_video(video_data, &id, output_format, codec).unwrap();

    println!("input len: {}", video_data.len());
    println!("compressed len: {}", compressed_video.len());

    let output_path = format!(
        "/home/pedro/projects/cmpressor/rust_compressor/assets/test_compressed.{}",
        output_format
    );
    println!("output path: {}", output_path);
    let mut output_file =
        std::fs::File::create(&output_path).expect("failed to create output file");

    output_file.write_all(&compressed_video).unwrap();

    let output_metadata = std::fs::metadata(&output_path).expect("failed to get metadata");
    assert!(output_metadata.is_file(), "output is not a file");
    assert!(output_metadata.len() > 0, "output file is empty");

    let output_extension = std::path::Path::new(&output_path)
        .extension()
        .and_then(std::ffi::OsStr::to_str)
        .expect("failed to get output file extension");
    assert_eq!(
        output_extension, output_format,
        "output file format does not match the expected format"
    );
}
