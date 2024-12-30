// result type for error handling, Rust style
public class Result<T>
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public T? Value { get; }

    private Result(bool isSuccess, string? errorMessage, T? value)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, null, value);
    public static Result<T> Failure(string errorMessage) => new(false, errorMessage, default);
}
