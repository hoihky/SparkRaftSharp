namespace SparkRaftSharp.Primitives;

/// <summary>Monotonically increasing election term. Zero is the initial term.</summary>
public readonly record struct RaftTerm(long Value)
{
    public static RaftTerm Zero => new(0);

    public RaftTerm Next => new(Value + 1);

    public override string ToString() => Value.ToString();
}
