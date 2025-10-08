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
            string? code = null,
            string? detail = null)
            : base(message, code, detail)
        {
            Resource = resource;
            ResourceId = resourceId;
        }
    }
}
