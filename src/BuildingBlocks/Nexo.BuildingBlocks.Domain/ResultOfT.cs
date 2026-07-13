namespace Nexo.BuildingBlocks.Domain;

/// <summary>
/// Represents the outcome of an operation that yields a <typeparamref name="T"/> value on success.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public new static Result<T> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<T>(T value) => Success(value);
}
