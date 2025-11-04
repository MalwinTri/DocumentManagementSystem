namespace DocumentManagementSystem.Infrastructure.Exceptions
{
    public sealed class MessagingException : Exception
    {
        public string Code { get; }
        public MessagingException(string message, string code, Exception? inner = null)
            : base(message, inner) => Code = code;
    }
}
