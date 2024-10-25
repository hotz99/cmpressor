using System;
using System.IO;
using Grpc.Net.Client;
using Compression; // Generated from compression.proto

var channel = GrpcChannel.ForAddress("https://localhost:50051");
var client = new CompressionService.CompressionServiceClient(channel);

// Read video file data
byte[] videoData = File.ReadAllBytes("path/to/uploaded/video.mp4");

// Create the request message
var request = new CompressRequest
{
  VideoData = Google.Protobuf.ByteString.CopyFrom(videoData),
  Codec = "h264"
};

// Call the Compress RPC
var response = await client.CompressAsync(request);

if (response.Success)
{
  Console.WriteLine("compression successful");
  // Handle the compressed video content
}
else
{
  Console.WriteLine($"compression failed: {response.Message}");
}

