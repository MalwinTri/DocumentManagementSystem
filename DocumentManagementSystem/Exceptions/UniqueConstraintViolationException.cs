namespace DocumentManagementSystem.Exceptions
{
    public sealed class UniqueConstraintViolationException : RepositoryException
    {
        public string? ConstraintName { get; }
        public object? Value { get; }

        public UniqueConstraintViolationException(
            string message = "Unique constraint violated",
            string? constraintName = null,
            object? value = null,
            string? code = null,
            string? detail = null,
            Exception? inner = null)
            : base(message, code, detail, inner)
        {
            ConstraintName = constraintName;
            Value = value;
        }
    }
}

