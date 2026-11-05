namespace MiniDpp.Api.Importing;

public sealed class InvalidCsvException : Exception
{
    public InvalidCsvException(string message) : base(message) { }
}
