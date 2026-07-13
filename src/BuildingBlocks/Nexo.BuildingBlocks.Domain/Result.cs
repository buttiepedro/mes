namespace Nexo.BuildingBlocks.Domain;

/// <summary>
/// Represents the outcome of an operation that either succeeds or fails with an <see cref="Domain.Error"/>.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}
