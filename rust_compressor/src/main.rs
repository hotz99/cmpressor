use compression_proto::{
    compression_service_server::{CompressionService, CompressionServiceServer},
    CompressRequest, CompressResponse,
};
use prost::Message;
use tonic::{transport::Server, Request, Response, Status};

pub mod compression_proto {
    tonic::include_proto!("compression");
    pub(crate) const FILE_DESCRIPTOR_SET: &[u8] =
        tonic::include_file_descriptor_set!("compression_service_descriptor");
}

#[derive(Default)]
pub struct MyCompressor;

#[tonic::async_trait]
impl CompressionService for MyCompressor {
    async fn compress(
        &self,
        request: Request<CompressRequest>,
    ) -> Result<Response<CompressResponse>, Status> {
        let req = request.into_inner();

        // placeholder for actual compression logic
        let compressed_data = perform_compression(&req.video_data, &req.codec)?;

        let response = CompressResponse {
            success: true,
            message: String::from("compression successful"),
            compressed_video: compressed_data,
        };

        Ok(Response::new(response))
    }
}

fn perform_compression(video_data: &str, codec: &str) -> Result<Vec<u8>, Status> {
    // Perform compression based on codec type
    // This is just a placeholder implementation
    //Ok(video_data.to_vec()) // In reality, replace with actual compressed data
    //
    Ok("hello from server".as_bytes().to_vec())
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let addr = "[::1]:50051".parse()?;
    let compressor = MyCompressor::default();

    println!("starting compression service ...");

    let reflection_service = tonic_reflection::server::Builder::configure()
        .register_encoded_file_descriptor_set(compression_proto::FILE_DESCRIPTOR_SET)
        .build_v1()
        .unwrap();

    Server::builder()
        .add_service(reflection_service)
        .add_service(CompressionServiceServer::new(compressor))
        .serve(addr)
        .await?;

    Ok(())
}
