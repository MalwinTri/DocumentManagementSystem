namespace DocumentManagementSystem.Exceptions
{
    public abstract class AppException : Exception
    {
        /// <summary>A short, machine-readable code the UI can branch on (optional).</summary>
        public string? Code { get; }

        /// <summary>Safe, client-facing detail (optional; if null, Message is used).</summary>
        public string? Detail { get; }

        protected AppException(string message, string? code = null, string? detail = null, Exception? inner = null)
            : base(message, inner)
        {
            Code = code;
            Detail = detail;
        }
    }
}
