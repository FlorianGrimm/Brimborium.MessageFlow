namespace Brimborium.MessageFlow.Internal;

internal static partial class LoggerExtensions {

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSourceConnectSource, EventName = nameof(LogMessageEvent.MessageSourceConnectSource),
        Level = LogLevel.Information,
        Message = "Connect from {SourceId} to {SinkId}")]
    public static partial void LogMessageSourceConnectSource(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier sinkId);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSourceDisconnectSource, EventName = nameof(LogMessageEvent.MessageSourceDisconnectSource),
    Level = LogLevel.Information,
    Message = "Disconnect from {SourceId} to {SinkId}")]
    public static partial void LogMessageSourceDisconnectSource(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier? sinkId);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSourceSendData, EventName = nameof(LogMessageEvent.MessageSourceSendData),
    Level = LogLevel.Information,
    Message = "MessageSource: Send Data from {SourceId} to{SinkId} Message:{Message}")]
    public static partial void LogMessageSourceSendData(this ILogger logger, Exception error, NodeIdentifier sourceId, NodeIdentifier sinkId, MessageLog message);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSourceSendControl, EventName = nameof(LogMessageEvent.MessageSourceSendControl),
    Level = LogLevel.Information,
    Message = "MessageSource: Send Control from {SourceId} to {SinkId}  Message:{Message}")]
    public static partial void LogMessageSourceSendControl(this ILogger logger, Exception error, NodeIdentifier sourceId, NodeIdentifier sinkId, MessageLog message);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSinkConnectionConnectSource, EventName = nameof(LogMessageEvent.MessageSinkConnectionConnectSource), 
        Level = LogLevel.Debug,
        Message = "MessageSink: Connect {SourceId} to {SinkId}")]
    public static partial void LogMessageSinkConnectionConnectSource(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier sinkId);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageSinkConnectionDisconnectSource, EventName = nameof(LogMessageEvent.MessageSinkConnectionDisconnectSource),
         Level = LogLevel.Debug,
         Message = "MessageSink: Disconnect {SourceId} to {SinkId}")]
    public static partial void LogMessageSinkConnectionDisconnectSource(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier? sinkId);


    [LoggerMessage(EventId = (int)LogMessageEvent.MessageConnectionSendData, EventName = nameof(LogMessageEvent.MessageConnectionSendData), 
        Level = LogLevel.Debug,
        Message = "MessageConnection: Send Data from {SourceId} to {SinkId} message {MessageLog}")]
    public static partial void LogMessageConnectionSendData(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier? sinkId, MessageLog messageLog);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageConnectionSendControl, EventName = nameof(LogMessageEvent.MessageConnectionSendControl), 
        Level = LogLevel.Debug,
        Message = "MessageConnection: Send Control from {SourceId} to {SinkId} message {MessageLog}")]
    public static partial void LogMessageConnectionSendControl(this ILogger logger, NodeIdentifier sourceId, NodeIdentifier? sinkId, MessageLog messageLog);

    [LoggerMessage(EventId = (int)LogMessageEvent.SinkLoggerDataMessage, EventName = nameof(LogMessageEvent.SinkLoggerDataMessage),
        Level = LogLevel.Information,
        Message = "SinkLogger: DataMessage {message}")]
    public static partial void LogSinkLoggerMessage(this ILogger logger, MessageLog message);

    [LoggerMessage(EventId = (int)LogMessageEvent.SinkLoggerControlMessage, EventName = nameof(LogMessageEvent.SinkLoggerControlMessage),
        Level = LogLevel.Information,
        Message = "SinkLogger: ControlMessage {message}")]
    public static partial void LogSinkLoggerControlMessage(this ILogger logger, MessageLog message);

    [LoggerMessage(EventId = (int)LogMessageEvent.MessageProcessorForwardControlFailed, EventName = nameof(LogMessageEvent.MessageProcessorForwardControlFailed),
        Level = LogLevel.Information,
        Message = "Forward Control {Message}")]
    public static partial void LogMessageProcessorForwardControlFailed(this ILogger logger, Exception error, MessageLog message);

    /*
    [LoggerMessage(EventId = (int)LogMessageEvent.X, EventName = nameof(LogMessageEvent.X), 
        Level = LogLevel.Information,
        Message = "1")]
    public static partial void LogX(this ILogger logger);
    */

}
internal enum LogMessageEvent {
    Start = 10100,
    MessageSinkConnectionConnectSource,
    MessageConnectionSendData,
    MessageConnectionSendControl,
    SinkLoggerDataMessage,
    SinkLoggerControlMessage,
    MessageSourceConnectSource,
    MessageSourceDisconnectSource,
    MessageSourceSendData,
    MessageSourceSendControl,
    MessageSinkConnectionDisconnectSource,
    MessageProcessorForwardControlFailed

}