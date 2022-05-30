using System;

namespace SqsPoller;

public class ExceptionDetails
{
    public Exception OriginalException { get; set; }
    public ExceptionType Type { get; set; }
    public string Message { get; set; }
}

public enum ExceptionType
{
    Read,
    Handle,
    Delete
}
