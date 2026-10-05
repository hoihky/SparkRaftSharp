using System.Text;
using System.Text.Json;
using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SimpleCluster.Networking;

public sealed class RaftWireCodec
{
    private readonly JsonSerializerOptions _options;

    public RaftWireCodec()
    {
        _options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _options.Converters.Add(new NodeIdJsonConverter());
        _options.Converters.Add(new LogIndexJsonConverter());
        _options.Converters.Add(new RaftTermJsonConverter());
        _options.Converters.Add(new RaftEntryJsonConverter());
        _options.Converters.Add(new ReadOnlyMemoryByteJsonConverter());
    }

    public byte[] Encode(IRaftMessage message)
    {
        var envelope = new WireEnvelope
        {
            Kind = message.Kind.ToString(),
            Payload = SerializePayload(message)
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, _options));
    }

    public IRaftMessage Decode(byte[] payload)
    {
        var envelope = JsonSerializer.Deserialize<WireEnvelope>(payload, _options)
                       ?? throw new InvalidOperationException("Invalid wire envelope.");

        return envelope.Kind switch
        {
            nameof(RaftMessageKind.RequestVote) => DeserializeRequestVote(envelope.Payload),
            nameof(RaftMessageKind.RequestVoteResult) => DeserializeRequestVoteResponse(envelope.Payload),
            nameof(RaftMessageKind.AppendEntries) => DeserializeAppendEntries(envelope.Payload),
            nameof(RaftMessageKind.AppendEntriesResult) => DeserializeAppendEntriesResponse(envelope.Payload),
            nameof(RaftMessageKind.InstallSnapshot) => DeserializeInstallSnapshot(envelope.Payload),
            nameof(RaftMessageKind.InstallSnapshotResult) => DeserializeInstallSnapshotResponse(envelope.Payload),
            _ => throw new NotSupportedException($"Unknown message kind: {envelope.Kind}")
        };
    }

    private JsonElement SerializePayload(IRaftMessage message) =>
        JsonSerializer.SerializeToElement(message, message.GetType(), _options);

    private RequestVoteRequest DeserializeRequestVote(JsonElement payload) =>
        JsonSerializer.Deserialize<RequestVoteRequest>(payload, _options)!;

    private RequestVoteResponse DeserializeRequestVoteResponse(JsonElement payload) =>
        JsonSerializer.Deserialize<RequestVoteResponse>(payload, _options)!;

    private AppendEntriesRequest DeserializeAppendEntries(JsonElement payload) =>
        JsonSerializer.Deserialize<AppendEntriesRequest>(payload, _options)!;

    private AppendEntriesResponse DeserializeAppendEntriesResponse(JsonElement payload) =>
        JsonSerializer.Deserialize<AppendEntriesResponse>(payload, _options)!;

    private InstallSnapshotRequest DeserializeInstallSnapshot(JsonElement payload) =>
        JsonSerializer.Deserialize<InstallSnapshotRequest>(payload, _options)!;

    private InstallSnapshotResponse DeserializeInstallSnapshotResponse(JsonElement payload) =>
        JsonSerializer.Deserialize<InstallSnapshotResponse>(payload, _options)!;

    private sealed class WireEnvelope
    {
        public string Kind { get; set; } = string.Empty;
        public JsonElement Payload { get; set; }
    }
}
