use std::io::Write;
use std::process::{Command, Stdio};

pub fn compress_video(
    video_data: &[u8],
    video_id: &str,
    format: &str,
    codec: &str,
) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
    // mp4 requires seekable output, which stdout is not
    // hence we write to a tmpfs (in-memory filesystem) file
    let output_file_path = format!("/tmp/{}.{}", video_id, format);

    let mut ffmpeg = Command::new("ffmpeg")
        .arg("-loglevel")
        .arg("error")
        .arg("-f")
        .arg(format)
        .arg("-i")
        .arg("-")
        .arg("-c:v")
        .arg(codec)
        .arg("-f")
        .arg(format)
        .arg(output_file_path.as_str())
        .stdin(Stdio::piped())
        .spawn()?;

    let stdin = ffmpeg.stdin.as_mut().unwrap();
    stdin.write_all(video_data)?;

    if let Err(err) = ffmpeg.wait() {
        return Err(format!("ffmpeg failed to perform compression: {}", err).into());
    } else {
        return Ok(std::fs::read(output_file_path)?);
    }
}

#[cfg(test)]

mod tests {
    use super::*;

    #[test]
    fn test_compress_video() {
        let video_data = include_bytes!("../assets/sample.mp4");
        let id = "id_1";
        let codec = "libx264";
        let format = "mp4";
        let compressed_video = compress_video(video_data, id, format, codec).unwrap();

        println!("input len: {}", video_data.len());
        println!("compressed len: {}", compressed_video.len());

        let output_path = format!(
            "/home/pedro/projects/cmpressor/rust_compressor/assets/sample_compressed.{}",
            format
        );
        println!("output path: {}", output_path);
        let mut output_file =
            std::fs::File::create(output_path).expect("failed to create output file");

        output_file.write_all(&compressed_video).unwrap();
    }
}
