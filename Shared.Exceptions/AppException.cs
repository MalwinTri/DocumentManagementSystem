namespace DocumentManagementSystem.Exceptions;
public abstract class AppException : Exception
{
    public string? Code { get; }
    public string? Detail { get; }

    protected AppException(string message, string? code = null, string? detail = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Detail = detail;
    }

    public override string ToString()
        => $"{GetType().Name}: {Message} (code={Code ?? "n/a"}){Environment.NewLine}{base.ToString()}";
}
