using RabbitmqRpc;
using Compression.Protobuf;

public class Handlers
{
  private readonly RpcProducer _rpcProducer;

  public Handlers(RpcProducer rpcProducer)
  {
    _rpcProducer = rpcProducer;
  }

  public async Task<IResult> HandleCompressionRequest(CompressionRequest req)
  {
    // TODO middleware to reject invalid/empty files
    if (req.VideoBytes.Length == 0)
    {
      return Results.BadRequest("no file was uploaded");
    }

    Console.WriteLine($"received file size (bytes): {req.VideoBytes.Length}");

    var compressionResponse = await _rpcProducer.CompressionCallAsync(req);

    Console.WriteLine($"compressed video size (bytes): {compressionResponse.CompressedVideoBytes.Length}");

    if (compressionResponse == null || !compressionResponse.Success)
    {
      return Results.BadRequest("failed to compress video: " + compressionResponse?.Error);
    }

    return Results.Bytes(compressionResponse.CompressedVideoBytes.ToByteArray(), "application/octet-stream");
  }
}
