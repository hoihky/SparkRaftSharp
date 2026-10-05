using SparkRaftSharp.Messaging;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal;

internal abstract record RaftEvent;

internal sealed record ElectionTimeoutEvent : RaftEvent;

internal sealed record HeartbeatTickEvent : RaftEvent;

internal sealed record InboundMessageEvent(IRaftMessage Message) : RaftEvent;

internal sealed record ProposeEvent(byte[] Data, TaskCompletionSource<LogIndex> Completion) : RaftEvent;

internal sealed record ResignLeadershipEvent : RaftEvent;

internal sealed record ShutdownEvent : RaftEvent;
