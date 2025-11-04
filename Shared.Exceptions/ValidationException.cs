namespace DocumentManagementSystem.Exceptions

{
    public sealed class ValidationException : AppException
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public ValidationException(
            string message = "Validation failed",
            IDictionary<string, string[]>? errors = null,
            string? code = "validation_error",
            string? detail = null)
            : base(message, code: code, detail: detail)
        {
            var normalized = (errors ?? new Dictionary<string, string[]>())
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);

            Errors = normalized;
        }
    }
}
