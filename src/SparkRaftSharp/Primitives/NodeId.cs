namespace SparkRaftSharp.Primitives;

/// <summary>Identifies a node in the Raft cluster.</summary>
public readonly record struct NodeId(string Value)
{
    public override string ToString() => Value;

    public static NodeId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Node id cannot be empty.", nameof(value));
        return new NodeId(value);
    }
}
