using DocumentManagementSystem.Exceptions;

namespace DocumentManagementSystem.DAL.Postgres.Exceptions;

public sealed class UniqueConstraintViolationException : RepositoryException
{
    public string? ConstraintName { get; }
    public object? Value { get; }

    public UniqueConstraintViolationException(
        string message = "Unique constraint violated",
        string? constraintName = null,
        object? value = null,
        string? entity = null,
        string? code = "unique_violation",
        string? detail = null,
        Exception? inner = null)
        : base(message,
               operation: "save_changes",
               entity: entity,
               code: code,
               detail: detail,
               inner: inner)
    {
        ConstraintName = constraintName;
        Value = value;
    }
}
