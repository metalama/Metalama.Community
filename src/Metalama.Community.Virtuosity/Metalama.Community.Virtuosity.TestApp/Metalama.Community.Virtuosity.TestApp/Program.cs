using Moq;

namespace Metalama.Community.Virtuosity.TestApp
{
    /// <summary>
    /// A standalone test: it consumes the Metalama.Community.Virtuosity NuGet package rather than a project
    /// reference, so it exercises the packaging itself - analyzer wiring, dependencies and MSBuild files - which
    /// the main solution cannot cover.
    /// </summary>
    internal class Program
    {
        public static int Main( string[] args )
        {
            // Test is declared sealed with a non-virtual method. Moq can only mock it if the aspect removed the
            // sealed modifier and made the method virtual, so this asserts on the woven package rather than
            // merely checking that the project compiles.
            var mock = new Mock<Test>();

            mock.Setup( foo => foo.Method() ).Returns( "Mock test" );

            var actual = mock.Object.Method();

            if ( actual != "Mock test" )
            {
                Console.Error.WriteLine(
                    $"FAIL: expected the mocked method to return 'Mock test' but it returned '{actual}'. "
                    + "The [Virtualize] aspect did not make Test.Method virtual." );

                return 1;
            }

            Console.WriteLine( "PASS: the sealed class was virtualized and could be mocked." );

            return 0;
        }
    }
}
