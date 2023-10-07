using System.Diagnostics.CodeAnalysis;

namespace DemoFile;

public class InvalidDemoException : Exception
{
    public InvalidDemoException(string message) : base(message)
    {
    }

    [DoesNotReturn]
    public static T Throw<T>(string message) => throw new InvalidOperationException(message);
}
