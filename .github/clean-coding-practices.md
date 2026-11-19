# Clean Coding Practices

## Purpose
Apply these rules to all new and modified code. Keep code tidy, modular, readable, and consistent.

## Naming Conventions
- Variables are nouns: `orderTotal`, `userName`.
- Functions/methods are verbs: `calculateTotal()`, `saveOrder()`.
- Booleans use positive names: `isValid`, `isLoading`.
- Prefer descriptive names over short cryptic names.
- Prefer singular names unless value is truly a collection.
- Use consistent casing:
  - `PascalCase` for classes/types.
  - `PascalCase` for public methods/properties in C#.
  - `camelCase` for locals/parameters.
- Most important naming rule: be consistent across the repository.

## Scope Indicators
- Private/protected members are prefixed with underscore: `_windowSize`. This includes `static` and `static readonly` fields (`_rootFields`, never `s_rootFields`); constants stay `PascalCase`.
- Enforced in the build: `.editorconfig` sets IDE1006 to `warning` and `EnforceCodeStyleInBuild` plus warnings-as-errors turn violations into build errors.
- Public members are named normally per C# conventions.
- Constants should be clearly recognizable and consistent project-wide.
  - If subsystem standard uses `UPPER_SNAKE_CASE`, follow it there.
  - For standard C# code, prefer `PascalCase` constants unless team rules say otherwise.

### Examples
```csharp
private int _windowSize = 900;
public int WindowSize { get; set; } = 900;

// Example only when subsystem standard requires UPPER_SNAKE_CASE:
private const int MAX_RETRY_COUNT = 3;
```

## Code Structure
- Keep functions small and single-responsibility.
- Keep classes focused; split large classes by responsibility.
- Avoid duplication; extract reusable logic.
- Reduce cyclomatic complexity by safeguarding with inverted `if` checks (guard clauses) and exiting early.
- Group related variables logically.
- Use whitespace to improve scanability.
- Comment intent and complex decisions; avoid obvious comments.
- Use standard iterators (`i`, `j`) for short loop scopes when appropriate.

## Documentation Comments
- Add XML documentation (`/// <summary>`) to every class, interface, enum, and struct.
- Document each public property, field, method, and enum member with a concise `<summary>`.
- Document public method parameters and return values with `<param>` and `<returns>`.
- Reference related types with `<see cref="..."/>` to aid navigation.
- Describe intent and behaviour, not the obvious (avoid restating the member name).
- Keep summaries short and single-purpose; expand only when behaviour is non-trivial.

### Example
```csharp
/// <summary>
/// Executes SQL commands and returns a structured execution result.
/// </summary>
public interface ISqlDriver
{
    /// <summary>Executes the supplied request and returns its outcome.</summary>
    /// <param name="request">The command definition and parameters to run.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <returns>A result describing success, data and any error details.</returns>
    Task<SqlExecutionResult> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken);
}
```

## Design Rules
- Avoid passing `null` values where possible; prefer explicit defaults/options.
- Avoid boolean control flags in method parameters; use explicit types or separate methods.
- Follow Command Query Separation (CQS):
  - Commands change state.
  - Queries return data and do not change state.
- Avoid hasty abstractions:
  - Start simple.
  - Extract abstractions only after repetition/complexity is clear.

## Architecture Rules
- Build small, focused components rather than monolithic components.
- Keep framework code distant from domain/application logic.
- Use abstraction layers and interfaces to decouple from framework details.
- Use dependency injection instead of direct framework object construction.
- Avoid framework-specific types in core domain code when possible.
- Keep clear boundaries between app code and framework/infrastructure code.
- Organize code by actor/responsibility where practical.

## Use Correct Constructs
- Use the right construct for the problem.
- Do not use C# `record` types; use normal `class` types instead.
- Keep control flow straightforward and easy to reason about.
- Avoid unnecessary complexity.
- Prefer arguments/return values over global state.
- Choose data structures and loops that improve clarity.

## Testing Standards
Tests must be:
- Fast
- Independent
- Deterministic
- Isolated from external systems (use mocks/stubs/fakes)
- Focused (one behavior per test)
- Easy to set up and tear down
- Reliable in local and CI environments
- Maintainable and readable
- Actionable when failing (clear failure messages)

Conventions:
- Name tests `Method_Scenario_ExpectedResult` (e.g. `Read_FileOverByteLimit_ReturnsLimitExceeded`).
- Give every `Assert.True`/`Assert.False` a message; prefer `Assert.Equal` on a tuple when one behaviour has several observable parts.
- Share expensive setup (child processes, hosts) through fixtures rather than asserting many behaviours in one test.
- Tag child-process suites `[Trait("Category", "Process")]` so they can be filtered (`--filter Category!=Process`).

## UI/CSS Baseline (When Applicable)
```css
a {
    text-decoration: none;
    color: #464feb;
}

tr th,
tr td {
    border: 1px solid #e6e6e6;
}

tr th {
    background-color: #f5f5f5;
}
```

## Quick Checklist
- Naming is descriptive and consistent.
- Methods/classes are small and focused.
- No obvious duplication.
- Classes, properties, methods and enums have XML `<summary>` documentation.
- Domain code is not tightly coupled to framework code.
- Methods follow CQS where applicable.
- Tests are fast and independent.
