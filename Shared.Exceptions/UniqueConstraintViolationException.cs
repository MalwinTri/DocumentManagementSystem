namespace DocumentManagementSystem.Exceptions;

public sealed class UniqueConstraintViolationException : AppException
{
    public string? ConstraintName { get; }
    public object? Value { get; }
    public string? Entity { get; }

    public UniqueConstraintViolationException(
        string message = "Unique constraint violated",
        string? constraintName = null,
        object? value = null,
        string? entity = null,
        string? code = "unique_violation",
        string? detail = null,
        Exception? inner = null)
        : base(message, code: code, detail: detail, inner: inner)
    {
        ConstraintName = constraintName;
        Value = value;
        Entity = entity;
    }
}
