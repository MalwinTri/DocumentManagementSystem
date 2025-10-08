namespace DocumentManagementSystem.Exceptions
{
    public class RepositoryException : AppException
    {
        public RepositoryException(
            string message = "Data access error",
            string? code = null,
            string? detail = null,
            Exception? inner = null)
            : base(message, code, detail, inner) { }
    }
}
