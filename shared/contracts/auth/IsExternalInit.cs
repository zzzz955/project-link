// Required polyfill for C# 9+ init accessors when targeting netstandard2.1.
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}
