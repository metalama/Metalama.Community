# Claude Instructions for Metalama.Community

## Prerequisites

Before starting work:

1. **Check plugins**: Verify the `eng@postsharp-engineering` and `metalama-dev@postsharp-engineering` plugins from `PostSharp.Engineering.AISkills` are available and load their _skills_ now. If not, ask the user to install them — they contain essential git workflow, build system, and release management knowledge.

   **IMPORTANT**: For git operations (commit, PR, merge) or when asked to "start working on issue X", ALWAYS read the `eng` skill first to get correct conventions. Don't attempt commits or PRs without consulting it.

   For writing aspects and templates, use the `metalama` skill. For developing the aspects in this repo, use `metalama-dev`.

2. **Check branch**: Before making any modifications, verify you're on a feature branch (`topic/YYYY.N/XXXX-description`). If on `develop/*` or `release/*`, propose creating/switching to a topic branch first.

## Repository Layout

This repo is a single product (`Metalama.Community`) with a **single solution**, `Metalama.Community.sln`. Each aspect lives under `src/`:

- `Metalama.Community.AutoCancellationToken`: propagates `CancellationToken` through async calls
- `Metalama.Community.Costura`: embeds dependencies into the assembly (Fody.Costura port)
- `Metalama.Community.Virtuosity`: makes members `virtual` without the keyword

Each aspect directory contains the aspect project, a `*.UnitTests`/`*.Tests` project, and usually a `*.Demo` or `*.TestApp`. `eng/src/Program.cs` defines the product (packages, SDK versions, public artifacts).

**Related repos** (in `..`):
- `Metalama`: the core framework this repo builds on
- `PostSharp.Engineering`: build orchestration SDK
- `Metalama.Documentation`: conceptual documentation

## Building and Testing

Because the main product is a **single solution**, `dotnet build` / `dotnet test` are normally sufficient. This repository is small, so **you may run `Build.ps1 build` and `Build.ps1 test` yourself** — a full build takes about 15 seconds and a full test run about 25 seconds. This overrides the `eng` skill rule that tells you to ask the user. Run them in the background anyway, and do not touch the working tree while they run.

Notes when a full build does happen:
- `Build.ps1 build` does not build test projects, only packable ones.
- `Build.ps1 test` implicitly does a clean rebuild, so do NOT chain it after `Build.ps1 build`. After a build, run test projects with `dotnet test <project> --no-build`.
- MSBuild binlogs land under `artifacts/logs`.
- After a failed build, run `Build.ps1 tools kill` to release file locks before retrying.

### Restore failures on NU1603

`error NU1603` at restore means the local artifacts of a dependency repository are older than the version this repository asks for. **Never suppress it** with `NoWarn`: the build then silently uses a different version of Metalama than the one it declares. Fix it instead:

1. `Build.ps1 dependencies update` — downloads the current artifacts of Metalama and Metalama.Compiler and rewrites `eng/Versions.Debug.g.props`.
2. `Build.ps1 build` (or `Build.ps1 prepare`) — regenerates `nuget.config`, which step 1 leaves untouched and which still points at the previous build directories.

Both steps are needed. After step 1 alone, restore still fails, and the message changes to name a different resolved version.

## Standalone Tests

A standalone test lives outside `Metalama.Community.sln`, consumes the produced NuGet package instead of a project reference, and therefore covers the packaging itself — analyzer wiring, dependencies, `.props`/`.targets`. Each one has its own directory with a `Directory.Build.props`, a `.sln`, a `README.md` and the project, and is registered as a `DotNetSolution` with `IsTestOnly = true` in `eng/src/Program.cs`. `Build.ps1 test` runs them; `dotnet test <solution>` runs one, after `Build.ps1 build` has produced the packages.

`dotnet test` needs something to invoke. Reference `Microsoft.NET.Test.Sdk` and override the `VSTest` target to run the application, as `Metalama.Community.Costura.ResolveRecursionTestApp` does, when the test is that the application runs and returns zero.

Existing standalone tests:
- `src\Metalama.Community.Virtuosity\Metalama.Community.Virtuosity.TestApp` — mocks a sealed class with Moq.
- `src\Metalama.Community.Costura\Metalama.Community.Costura.ResolveRecursionTestApp` — the assembly-resolve recursion of issue #113.

## Aspect Tests

`Metalama.Community.Virtuosity.UnitTests` and `Metalama.Community.AutoCancellationToken.UnitTests` are **snapshot aspect tests** using `Metalama.Testing.AspectTesting`. `Metalama.Community.Costura.Tests` is a classic xUnit project.

- Each test is a `Foo.cs` (input) / `Foo.t.cs` (expected transformed output) pair. There is no `metalamaTests.json` and no runner class — tests are discovered from the `.cs` files compiled into the assembly.
- Run them with `dotnet test` from the test project directory. Filter by the **bare file name**: `dotnet test --filter "AbstractClass"`.
- On failure, the actual output is written to `obj/transformed/<tfm>/Foo.t.cs` and echoed in the test output. Update a baseline by copying that file over the expected `.t.cs`.
- The `.t.cs` baseline is **not** a verbatim copy of the input: the copyright header and any `#pragma` preceding the `namespace` are stripped, and the code is reformatted (2-space indent, braces on their own lines).
- Compilation errors in the transformed code appear as `// Error CSxxxx on ...` comments at the top of the actual output, and fail the test. This makes aspect tests a reliable way to reproduce a "generated code doesn't compile" bug **before** fixing it — commit the failing test first (see #94, #98).

## Debugging Build Issues

1. **Check troubleshooting files**: Look at `%TEMP%\Metalama\CompileTimeTroubleshooting\...\errors.txt` for actual errors.
2. **Trace data flow**: For MSBuild issues, trace from `.csproj` → `.targets` → Engine code.

## Working on GitHub Issues

1. Read all details about the issue online.
2. Check conceptual documentation under `../Metalama.Documentation/content`, plus the aspect's own `README.md` and `Details.md` — user-visible behavior changes usually belong there.
3. Create a branch: `topic/YYYY.N/XXXX-short-description` (merge target is always `develop/YYYY.N`, never the release branch).
4. Mark the issue status **In Progress** and make sure it is assigned to the current user.
5. Track progress in a `<issue-number>-TODO.md` file. **Do not commit `*-TODO.md`.**
6. Create issues promptly when discovering bugs during development.

### Pull requests

Follow the `/eng:create-pr` skill. One addition it omits: GitHub only creates the issue link if the body contains a **closing keyword** (`Fixes #98`), not a bare `#98` reference. Include the keyword *before* running the base-branch toggle workaround, then verify with `closingIssuesReferences`.

## Conventions

- **Never sign commits.** GitHub comments, issues, and PRs are signed `— Claude for <user-name>` — no ad link.
- In tests, never use hardcoded delays; use barriers, `TaskCompletionSource`, or sync points.
- Never `await` without a cancellation token — ever.
- Don't lose time on cosmetic warnings (such as redundant usings) until the finalizing stage of a commit.
- When adding a package reference, also add the `PackageVersion` to `Directory.Packages.props`.

## Container Environment

When running in a container (`DockerBuild.ps1 -Claude`), `gh` is not available, and `git push` / `git fetch` over the network do not work either. **All GitHub network operations (push, fetch from remote, gh API calls, etc.) MUST go through the host-approval MCP server (`mcp__host-approval__execute_command`).**

**If the MCP server is unavailable** (e.g., disconnected, the deferred-tool reminder says it's gone): STOP. Tell the user MCP is unavailable and ask them to run the command from the host. **Do NOT fall back to direct `git push` / `gh` via Bash** — it will fail, and bypassing MCP violates the human-in-the-loop policy. Treat MCP-unavailability as a policy boundary, not a tool-selection problem.

## Shell

Two shells are available and they take **different syntax**. PowerShell here-strings (`@'...'@`) are a silent corruption hazard in the Bash tool: it accepts them as literal text, so a commit message ends up wrapped in stray `@` lines instead of failing loudly. Use a heredoc (`git commit -F - <<'EOF'`) in Bash, or the PowerShell tool for `@'...'@`. Read the message back (`git log -1 --format=%B`) when it matters.
