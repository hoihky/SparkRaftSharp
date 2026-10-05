namespace SparkRaftSharp.Primitives;

/// <summary>1-based index into the replicated log. Zero means none / invalid.</summary>
public readonly record struct LogIndex(long Value)
{
    public static LogIndex Zero => new(0);

    public bool IsValid => Value > 0;

    public LogIndex Next => new(Value + 1);

    public static LogIndex Min(LogIndex a, LogIndex b) =>
        a.Value <= b.Value ? a : b;

    public override string ToString() => Value.ToString();
}
