using System.Collections.Concurrent;
using System.Text;
using Google.Protobuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Compression.Protobuf;

namespace RabbitmqRpc;

public class RpcProducer : IAsyncDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IConnectionFactory _connectionFactory;
    // correlationId -> TaskCompletionSource
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CompressionResponse>> _callbackMapper = new();

    private IConnection? _connection;
    private IChannel? _channel;
    private string? _replyQueueName;

    public RpcProducer()
    {
        _connectionFactory = new ConnectionFactory { HostName = "localhost" };
    }

    public async Task StartAsync()
    {
        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        var queueDeclareResult = await _channel.QueueDeclareAsync();
        _replyQueueName = queueDeclareResult.QueueName;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            if (!string.IsNullOrEmpty(ea.BasicProperties.CorrelationId) &&
                _callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
            {
                var response = CompressionResponse.Parser.ParseFrom(ea.Body.ToArray());
                tcs.TrySetResult(response);
            }
            await Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
    }

    public async Task<CompressionResponse> CompressionCallAsync(
        CompressionRequest request, CancellationToken cancellationToken = default)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Channel is not initialized. Call StartAsync() first.");
        }

        string correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName
        };

        var tcs = new TaskCompletionSource<CompressionResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, tcs);

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QUEUE_NAME,
            mandatory: true,
            basicProperties: props,
            body: request.ToByteArray());

        cancellationToken.Register(() =>
        {
            _callbackMapper.TryRemove(correlationId, out _);
            tcs.SetCanceled();
        });

        return await tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }
}

