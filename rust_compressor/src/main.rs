use std::env;

pub mod compression;
mod rabbitmq_rpc_consumer;

pub mod compression_protobuf {
    include!(concat!(env!("OUT_DIR"), "/compression.protobuf.rs"));
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let rabbitmq_user = env::var("RABBITMQ_USER").expect("failed to read RABBITMQ_USER env var");
    let rabbitmq_password =
        env::var("RABBITMQ_PASSWORD").expect("failed to read RABBITMQ_PASSWORD env var");
    let rabbitmq_host = env::var("RABBITMQ_HOST").expect("failed to read RABBITMQ_HOST env var");
    let rabbitmq_port = env::var("RABBITMQ_PORT").expect("failed to read RABBITMQ_PORT env var");
    let rabbitmq_uri = format!(
        "amqp://{}:{}@{}:{}",
        rabbitmq_user, rabbitmq_password, rabbitmq_host, rabbitmq_port
    );

    //let rabbitmq_uri = "amqp://guest:guest@localhost:5672";

    println!("rabbitmq uri: {}", rabbitmq_uri);

    let rabbitmq_connection =
        lapin::Connection::connect(&rabbitmq_uri, lapin::ConnectionProperties::default()).await?;

    rabbitmq_rpc_consumer::receive(&rabbitmq_connection).await?;

    Ok(())
}
