namespace SparkRaftSharp.Time;

public interface IElectionTimeoutJitter
{
    TimeSpan NextDelay(TimeSpan minimum, TimeSpan maximum);
}
