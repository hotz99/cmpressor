mod compression;
mod rabbitmq;

//pub mod compression_proto {
//    tonic::include_proto!("compression");
//    pub(crate) const FILE_DESCRIPTOR_SET: &[u8] =
//        tonic::include_file_descriptor_set!("compression_service_descriptor");
//}
//
//#[derive(Default)]
//pub struct MyCompressor;
//
//#[tonic::async_trait]
//impl compression_proto::service_server::Service for MyCompressor {
//    async fn compress(
//        &self,
//        request: tonic::Request<compression_proto::Request>,
//    ) -> Result<tonic::Response<compression_proto::Response>, tonic::Status> {
//        let req = request.into_inner();
//
//        let codec_str = match compression_proto::Codec::try_from(req.codec) {
//            Ok(compression_proto::Codec::H264) => "libx264",
//            Ok(compression_proto::Codec::H265) => "libx265",
//            Err(_) => return Err(tonic::Status::invalid_argument("invalid codec specified")),
//        };
//
//        let start = std::time::Instant::now();
//
//        match compression::compress_video(&req.video_data, codec_str) {
//            Ok(compressed_data) => {
//                println!("compression took: {:?}", start.elapsed());
//                println!("input data size: {}", req.video_data.len());
//                println!("compressed data size: {}", compressed_data.len());
//
//                let response = compression_proto::Response {
//                    success: true,
//                    message: None,
//                    compressed_video: Some(compressed_data),
//                };
//                Ok(tonic::Response::new(response))
//            }
//            Err(err) => {
//                eprintln!("error during compression: {}", err);
//
//                let response = compression_proto::Response {
//                    success: false,
//                    message: Some(err.to_string()),
//                    compressed_video: None,
//                };
//                Ok(tonic::Response::new(response))
//            }
//        }
//    }
//}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    //let addr = "[::1]:50051".parse()?;
    //let compressor = MyCompressor::default();

let rabbitmq_connection = lapin::Connection::connect(
            // TODO move to env var
            "amqp://guest:guest@localhost:5672",
            lapin::ConnectionProperties::default(),
        )
        .await?;

    let mut rpc_consumer = rabbitmq::RpcConsumer::new(&rabbitmq_connection).await?;
    rpc_consumer.listen().await?;

    println!("starting compression service ...");

    //let reflection_service = tonic_reflection::server::Builder::configure()
    //    .register_encoded_file_descriptor_set(compression_proto::FILE_DESCRIPTOR_SET)
    //    .build_v1()
    //    .unwrap();
    //
    //tonic::transport::Server::builder()
    //    .add_service(reflection_service)
    //    .add_service(compression_proto::service_server::ServiceServer::new(
    //        compressor,
    //    ))
    //    .serve(addr)
    //    .await?;

    Ok(())
}
