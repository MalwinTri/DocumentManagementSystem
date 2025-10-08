namespace DocumentManagementSystem.Exceptions
{
    public sealed class ValidationException : AppException
    {
        /// <summary>Optional per-field errors if you have them.</summary>
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(
            string message = "Validation failed",
            IDictionary<string, string[]>? errors = null,
            string? code = null,
            string? detail = null)
            : base(message, code, detail)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }
}
