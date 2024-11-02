using System.Collections.Concurrent;
using Google.Protobuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Compression.Protobuf;

namespace RabbitmqRpc;

public class RpcProducer : IDisposable
{
  private const string QUEUE_NAME = "rpc_queue";

  private readonly ConnectionFactory _factory;
  private readonly IConnection _connection;
  private readonly IModel _channel;
  private readonly string _replyQueueName;
  private readonly EventingBasicConsumer _consumer;
  // correlationId -> TaskCompletionSource 
  private readonly ConcurrentDictionary<string, TaskCompletionSource<CompressionResponse>> callbackMap = new();

  public RpcProducer(ConnectionFactory factory)
  {
    try
    {
      _factory = factory;
      _connection = _factory.CreateConnection();
    }
    catch (Exception e)
    {
      Console.WriteLine("failed to connect to rabbitmq. is the server running ?");
      Console.WriteLine(e.Message);
      throw;

    }
    _channel = _connection.CreateModel();
    _replyQueueName = _channel.QueueDeclare().QueueName;

    // rpcproducer is a consumer of the reply queue
    _consumer = new EventingBasicConsumer(_channel);
    _consumer.Received += (model, ea) =>
    {
      Console.WriteLine("received response from rabbitmq");

      // TODO what ?
      if (callbackMap.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
      {
        tcs.SetResult(CompressionResponse.Parser.ParseFrom(ea.Body.ToArray()));
      }
    };
    _channel.BasicConsume(consumer: _consumer, queue: _replyQueueName, autoAck: true);
  }

  public void Dispose()
  {
    _channel.Dispose();
  }

  public Task<CompressionResponse> CompressionCallAsync(IFormFile file, CancellationToken cancellationToken = default)
  {
    var correlationId = Guid.NewGuid().ToString();
    var tcs = new TaskCompletionSource<CompressionResponse>();

    try
    {
      callbackMap.TryAdd(correlationId, tcs);
    }
    catch (Exception e)
    {
      Console.WriteLine("failed to add task to callback map");
      Console.WriteLine(e.Message);
      throw;
    }

    // TODO try directly casting file as byte array
    using var fileStream = file.OpenReadStream();
    byte[] videoBytes = new byte[file.Length];
    fileStream.Read(videoBytes, 0, (int)file.Length);

    var compressionRequest = new CompressionRequest
    {
      VideoBytes = ByteString.CopyFrom(videoBytes),
      Codec = Codec.H264,
    };

    IBasicProperties props = _channel.CreateBasicProperties();
    props.CorrelationId = correlationId;
    props.ReplyTo = _replyQueueName;

    _channel.BasicPublish(exchange: string.Empty,
                         routingKey: QUEUE_NAME,
                         basicProperties: props,
                         body: compressionRequest.ToByteArray());

    Console.WriteLine("sent compression request to rabbitmq");

    // TODO what ?
    cancellationToken.Register(() => callbackMap.TryRemove(correlationId, out _));

    return tcs.Task;
  }
}
