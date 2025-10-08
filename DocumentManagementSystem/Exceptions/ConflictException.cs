namespace DocumentManagementSystem.Exceptions
{
    public sealed class ConflictException : AppException
    {
        public ConflictException(string message = "Conflict", string? code = null, string? detail = null, Exception? inner = null)
            : base(message, code, detail, inner) { }
    }
}
