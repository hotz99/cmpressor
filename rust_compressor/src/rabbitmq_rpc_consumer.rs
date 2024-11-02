use lapin::{
    options::{BasicAckOptions, BasicConsumeOptions, BasicPublishOptions, QueueDeclareOptions},
    types::FieldTable,
    BasicProperties, Connection,
};
use std::sync::Arc;
use tokio::sync::Mutex;

use crate::compression;
use crate::compression_protobuf;

const QUEUE_NAME: &str = "rpc_queue";
const CONSUMER_TAG: &str = "rust_rpc_worker";

// boxed error as a dynamic dispatch implementation allows us to return any error type (that
// implemtent Error trait) at runtime
pub async fn receive(connection: &Connection) -> Result<(), Box<dyn std::error::Error>> {
    let channel = connection.create_channel().await?;

    let queue = channel
        .queue_declare(
            QUEUE_NAME,
            QueueDeclareOptions::default(),
            FieldTable::default(),
        )
        .await?;

    println!(
        "waiting for rpc requests on queue: {}",
        queue.name().as_str()
    );

    let mut consumer = channel
        .basic_consume(
            queue.name().as_str(),
            CONSUMER_TAG,
            BasicConsumeOptions::default(),
            FieldTable::default(),
        )
        .await?;

    // shared channel between threads allocated for each request
    let channel = Arc::new(Mutex::new(channel));

    use futures_util::stream::StreamExt;
    while let Some(delivery_result) = consumer.next().await {
        if let Ok(delivery) = delivery_result {
            delivery
                .ack(BasicAckOptions::default())
                .await
                .expect("failed to acknowledge message");

            let reply_to = delivery
                .properties
                .reply_to()
                .clone()
                .expect("reply_to not set");

            use prost::Message;
            let compression_request =
                compression_protobuf::CompressionRequest::decode(delivery.data.as_slice())
                    .expect("failed to decode protobuf");
            let correlation_id = delivery
                .properties
                .correlation_id()
                .clone()
                .expect("correlation_id not set");

            // TODO propagate error message from handler
            let compression_response =
                handle_compression_request(compression_request, correlation_id.as_str()).await?;

            let channel = Arc::clone(&channel);
            let publish_channel = channel.lock().await;

            publish_channel
                .basic_publish(
                    "",
                    &reply_to.as_str(),
                    BasicPublishOptions::default(),
                    &compression_response.encode_to_vec(),
                    BasicProperties::default().with_correlation_id(correlation_id),
                )
                .await?;
        }
    }

    Ok(())
}

async fn handle_compression_request(
    request: compression_protobuf::CompressionRequest,
    correlation_id: &str,
) -> Result<compression_protobuf::CompressionResponse, Box<dyn std::error::Error>> {
    let format = "mp4";
    let codec = match request.codec {
        0 => "libx264",
        1 => "libx265",
        _ => {
            return Err("invalid codec".into());
        }
    };

    println!("input data size (bytes): {}", request.video_bytes.len());

    let start = std::time::Instant::now();

    match compression::compress_video(&request.video_bytes, correlation_id, format, codec) {
        Ok(bytes) => {
            println!("compression took: {:?}", start.elapsed());
            println!("input data size: {}", request.video_bytes.len());
            println!("compressed data size: {}", bytes.len());

            let compression_response = compression_protobuf::CompressionResponse {
                success: true,
                message: None,
                compressed_video_bytes: Some(bytes),
            };

            Ok(compression_response)
        }
        Err(err) => {
            eprintln!("error during compression: {}", err);

            Err(err)
        }
    }
}
