using RabbitmqRpc;

public class Handlers
{
  private readonly RpcProducer _rpcProducer;

  public Handlers(RpcProducer rpcProducer)
  {
    _rpcProducer = rpcProducer;
  }

  public async Task<IResult> HandleCompressionRequest(IFormFile file)
  {
    Console.WriteLine("compression request received");

    // TODO middleware to reject invalid/empty files
    if (file == null || file.Length == 0)
    {
      return Results.BadRequest("no file was uploaded or file is empty");
    }

    Console.WriteLine($"received file size (bytes): {file.Length}");

    var compressionResponse = await _rpcProducer.CompressionCallAsync(file);

    Console.WriteLine($"compressed video size (bytes): {compressionResponse.CompressedVideoBytes.Length}");

    if (compressionResponse == null || !compressionResponse.Success)
    {
      return Results.BadRequest("failed to compress video: " + compressionResponse?.Error);
    }

    return Results.Bytes(compressionResponse.CompressedVideoBytes.ToByteArray(), "application/octet-stream");
  }
}
