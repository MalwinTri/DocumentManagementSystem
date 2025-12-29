namespace DocumentManagementSystem.Exceptions
{
    public sealed class OperationFailedException : AppException
    {
        public OperationFailedException(
            string message,
            string? code = null,
            string? detail = null,
            Exception? inner = null)
            : base(message, code, detail, inner)
        { }
    }
}
