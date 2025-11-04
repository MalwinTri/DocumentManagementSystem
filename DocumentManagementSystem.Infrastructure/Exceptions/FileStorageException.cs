namespace DocumentManagementSystem.Infrastructure.Exceptions
{
    public sealed class FileStorageException : Exception
    {
        public string Code { get; }
        public FileStorageException(string message, string code, Exception? inner = null)
            : base(message, inner) => Code = code;
    }
}
