namespace DocumentManagementSystem.Exceptions;

public sealed class ConflictException : AppException
{
    public string? Resource { get; }
    public object? ResourceId { get; }

    public ConflictException(
        string message = "Conflict",
        string? code = "conflict",
        string? detail = null,
        Exception? inner = null)
        : base(message, code, detail, inner) { }

    public ConflictException(
        string resource,
        object? resourceId,
        string? detail = null,
        Exception? inner = null)
        : base($"{resource} '{resourceId}' is in a conflicting state", "conflict", detail, inner)
    {
        Resource = resource;
        ResourceId = resourceId;
    }
}
