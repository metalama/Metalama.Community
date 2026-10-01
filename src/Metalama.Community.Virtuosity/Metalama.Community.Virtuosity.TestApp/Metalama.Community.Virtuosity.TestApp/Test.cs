// This is an open-source Metalama example. See https://github.com/postsharp/Metalama.Samples for more.

using Metalama.Community.Virtuosity;

namespace Metalama.Community.Virtuosity.TestApp
{
    [Virtualize]
    public sealed class Test
    {
        public string Method()
        {
            return "Test.Method";
        }
    }
}