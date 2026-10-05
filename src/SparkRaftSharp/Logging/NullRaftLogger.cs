namespace SparkRaftSharp.Logging;

public sealed class NullRaftLogger : IRaftLogger
{
    public void Debug(string message) { }

    public void Info(string message) { }

    public void Warn(string message) { }

    public void Error(string message, Exception? exception = null) { }
}
