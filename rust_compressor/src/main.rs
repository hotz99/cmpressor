mod compression;
mod rabbitmq_rpc_consumer;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let rabbitmq_connection = lapin::Connection::connect(
        // TODO move to env var
        "amqp://guest:guest@localhost:5672",
        lapin::ConnectionProperties::default(),
    )
    .await?;

    rabbitmq_rpc_consumer::receive(&rabbitmq_connection).await?;

    Ok(())
}
