namespace DocumentManagementSystem.Exceptions
{
    public class RepositoryException : AppException
    {
        public string? Operation { get; }
        public string? Entity { get; }

        public RepositoryException(
            string message = "Data access error",
            string? operation = null,
            string? entity = null,
            string? code = "repository_error",
            string? detail = null,
            Exception? inner = null)
            : base(message, code, detail, inner)
        {
            Operation = operation;
            Entity = entity;
        }
        public static RepositoryException Query(string entity, string? detail = null, Exception? inner = null) =>
            new($"Data access error while querying {entity}", operation: "query", entity: entity, detail: detail, inner: inner);

        public static RepositoryException Save(string entity, string? detail = null, Exception? inner = null) =>
            new($"Failed to persist {entity}", operation: "save_changes", entity: entity, detail: detail, inner: inner);
    }
}
