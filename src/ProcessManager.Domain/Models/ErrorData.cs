namespace ProcessManager.Domain.Models
{
    public class ErrorData
    {
        public ErrorData(string message, string code, string target)
        {
            Message = message;
            Code = code;
            Target = target;
        }

        public string Message { get; }
        public string Code { get; }
        public string Target { get; }
    }
}
