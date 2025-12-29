namespace DocumentManagementSystem.Exceptions
{
    public sealed class NotFoundException : AppException
    {
        public string? Resource { get; }
        public object? ResourceId { get; }

        public NotFoundException(
            string message = "Resource not found",
            string? resource = null,
            object? resourceId = null,
            string? code = "not_found",
            string? detail = null,
            Exception? inner = null)
            : base(message, code, detail, inner)
        {
            Resource = resource;
            ResourceId = resourceId;
        }

        public static NotFoundException For<T>(object? id, string? detail = null, Exception? inner = null)
            => new(resource: typeof(T).Name, resourceId: id, detail: detail, inner: inner);
    }
}
