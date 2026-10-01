// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Community.Virtuosity.Demo;
using Moq;

var mock = new Mock<Foo>();

mock.Setup( foo => foo.Bar() ).Returns( "Mocked!" );

// If the mock succeeded, this should print "Mocked!".
Console.WriteLine( mock.Object.Bar() );