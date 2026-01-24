namespace DocumentManagementSystem.Infrastructure.Exceptions
{
    public sealed class AiRateLimitException : Exception
    {
        public TimeSpan RetryAfter { get; }

        public AiRateLimitException(TimeSpan retryAfter, string message)
            : base(message)
        {
            RetryAfter = retryAfter;
        }
    }
}
