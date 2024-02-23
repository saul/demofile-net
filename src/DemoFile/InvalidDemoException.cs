using System.Diagnostics.CodeAnalysis;

namespace DemoFile;

public class InvalidDemoException : Exception
{
    public InvalidDemoException(string message) : base(message)
    {
    }
}
