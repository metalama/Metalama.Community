# Costura assembly-resolve recursion

A standalone test for [issue #113](https://github.com/metalama/Metalama.Community/issues/113). It consumes the
`Metalama.Community.Costura` NuGet package rather than a project reference, and it runs the woven application instead
of inspecting the runtime template, so it covers the failure the way a user meets it.

## The bug

`DependencyExtractor.ReadExistingAssembly` runs inside the `AppDomain.AssemblyResolve` handler that Costura installs.
It identified the loaded assemblies with `Assembly.GetName()`, which on .NET Framework issues a
`FileIOPermission` path-discovery demand on the assembly code base. When that demand is refused, the security engine
builds the message of the `SecurityException` from the `Security_Generic` resource key of mscorlib. On a UI culture
other than the neutral one, that lookup loads a satellite assembly, which raises `AssemblyResolve` again, which calls
`ReadExistingAssembly` again, which calls `Assembly.GetName()` again. mscorlib detects the recursive lookup of the
same resource key, reports `[mscorlib recursive resource lookup bug]` and kills the process.

`Assembly.FullName` reports the same simple name, version and culture from a cached string and issues no demand.

## What the test does

The application embeds `Newtonsoft.Json` with Costura, then, in a child process:

1. Sets the UI culture to `de-DE`, so that a resource lookup in mscorlib goes looking for a satellite assembly.
2. Restricts the stack with `PermitOnly` to a permission set that grants no `FileIOPermission`. The process stays
   fully trusted; any `PermitOnly` or `Deny` frame refuses the path-discovery demand the same way.
3. Calls into `Newtonsoft.Json`, which raises `AssemblyResolve` and lands in the Costura handler.

**Bug present**: the child process dies with `COR_E_FAILFAST` (0x80131623), or waits on the mscorlib assert dialog
that precedes it.
**Bug absent**: the embedded assembly is resolved, the child process prints the serialized JSON and returns zero.

The reproduction runs in a child process because the failure kills the process, and on a desktop of its own because
mscorlib opens a modal dialog before it calls `Environment.FailFast`. Nothing is shown to the user: the parent
process reads the text of that dialog, reports it on the console and kills the child.

## How to run

`Build.ps1 test` runs it, because the solution is registered in `eng/src/Program.cs`. To run it by hand after
`Build.ps1 build`, run `dotnet test Metalama.Community.Costura.ResolveRecursionTestApp.sln`, or run the built
executable directly.
