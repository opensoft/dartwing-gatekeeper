namespace DartWing.DomainModel;

public sealed class DartWingResponse<T>
{
    public bool IsSuccess { get; }
    public T? Result { get; }
    public string? Message { get; }
    public Exception? Exception { get; }

    public DartWingResponse(T result)
    {
        IsSuccess = true;
        Result = result;
    }

    public DartWingResponse(string message, Exception? exception = null)
    {
        IsSuccess = false;
        Message = message;
        Exception = exception;
    }
}