using System.Text.Json;
using System.Text.Json.Serialization;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SimpleCluster.Networking;

internal sealed class NodeIdJsonConverter : JsonConverter<NodeId>
{
    public override NodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("Node id expected."));

    public override void Write(Utf8JsonWriter writer, NodeId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

internal sealed class LogIndexJsonConverter : JsonConverter<LogIndex>
{
    public override LogIndex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, LogIndex value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}

internal sealed class RaftTermJsonConverter : JsonConverter<RaftTerm>
{
    public override RaftTerm Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, RaftTerm value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}

internal sealed class RaftEntryJsonConverter : JsonConverter<RaftEntry>
{
    public override RaftEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        LogIndex index = LogIndex.Zero;
        RaftTerm term = RaftTerm.Zero;
        byte[] data = Array.Empty<byte>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            var prop = reader.GetString() ?? string.Empty;
            reader.Read();
            switch (prop)
            {
                case "index":
                    index = new LogIndex(reader.GetInt64());
                    break;
                case "term":
                    term = new RaftTerm(reader.GetInt64());
                    break;
                case "data":
                    data = reader.GetBytesFromBase64() ?? Array.Empty<byte>();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new RaftEntry(index, term, data);
    }

    public override void Write(Utf8JsonWriter writer, RaftEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("index", value.Index.Value);
        writer.WriteNumber("term", value.Term.Value);
        writer.WriteBase64String("data", value.Data.Span);
        writer.WriteEndObject();
    }
}

internal sealed class ReadOnlyMemoryByteJsonConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetBytesFromBase64() ?? Array.Empty<byte>();

    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> value, JsonSerializerOptions options) =>
        writer.WriteBase64StringValue(value.Span);
}
