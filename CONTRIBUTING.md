# Contributing to router-quack

First off, thank you for considering contributing to router-quack!

## How Can I Contribute?

### Reporting Bugs

1. Check the [Issues](https://github.com/Tuasco/router-quack/issues) to see if the bug has already been reported.
1. If not, open a new issue using our **Bug Report** template.
1. Please include your intent file(s).

### Suggesting Enhancements

1. Whether it's a new CLI flag, support for a new vendor (e.g., VyOS, MikroTik), or something else, your ideas are
   welcome.
1. Open an [issue](https://github.com/Tuasco/router-quack/issues) using our **Feature Request** template
   to discuss the design before diving into the code.

### Pull Requests

We follow the **Fork and Pull** model:

1. **Fork** the repository to your own account.
1. **Clone** your fork locally.
1. **Create a branch** for your fix/feature (e.g., `git switch -c feature/new-vendor-support`).
1. **Commit** your changes with clear, descriptive messages.
1. **Push** to your fork and submit a **Pull Request** against our `main` branch.

### Commit messages

If you want a change to be mentioned in the release notes, it must start with a requarks type.
Accepted types are : `feature`, `fix`, `perf`, `refactor`, `test`, `chore` and `ci`.

## Project Architecture

### Core Interfaces

The project follows a pipeline architecture with steps implementing specific interfaces:

#### IStep (Base Interface)

All pipeline steps implement `IStep`, which provides:
- `Context`: Shared data and configuration
- `Logger`: For logging messages
- `BeginMessage`: Optional message logged at step start

#### Child Interfaces

- **`IIntentFileParser`**: Reads and parses intent files
    - Implement this for new file format support
  - Examples: YAML intent files parser
- **`IValidator`**: Validates parsed configuration
    - Implement this for new validation rules
    - Examples: `NoDuplicateRouterNames`, `ValidNetworkSpaces`
- **`IProcessor`**: Processes and transforms configuration data
    - Implement this for new processing stages
    - Examples: Configuration generation, data transformation
- **`IConfigFileWriter`**: Writes the parsed configuration
    - Implement this for new vendor support
    - Examples: Cisco writer

### Adding New Features

1. **Create a new step** implementing the appropriate interface
2. **Register in DI container** at `src/RouterQuack.CLI/Startup/DependencyInjection.cs`
3. **Add to the pipeline** in the appropriate execution order
4. **Write tests** in `tests/RouterQuack.Tests/`

### Project Structure

```
router-quack/
├── src/
│   ├── RouterQuack.CLI/          # Command-line interface
│   ├── RouterQuack.Core/         # Core business logic
│   └── RouterQuack.IO.Yaml/      # YAML parsing
│   └── RouterQuack.IO.Cisco/     # Cisco config generation
└── tests/
    └── RouterQuack.Tests/        # Unit tests
```

## Local Development Setup

### Stack

- **Framework:** .NET 10.0 / C# 14
- **IDE:** JetBrains Rider, Visual Studio, or VS Code with C# Dev Kit
- **Testing Framework:** TUnit with NSubstitute for mocking

### Build Instructions

To build the entire solution from the root directory:

```bash
dotnet build
```

To build the CLI project specifically:

```bash
dotnet build src/RouterQuack.CLI/RouterQuack.CLI.csproj
```

To run the CLI:

```bash
dotnet run --project src/RouterQuack.CLI -- [flags]
```

**Note:** The project has nullable reference types enabled.
Pay attention to any nullability warnings during compilation.

### Testing

Run all tests from the root directory:

```bash
dotnet test
```

Tests are located in `tests/RouterQuack.Tests/` and use the TUnit testing framework with NSubstitute for mocking.
When contributing:
- Add tests for new features
- Ensure existing tests pass before submitting a PR
- Write tests that cover both success and failure cases

## Coding Standards

### Automatic Formatting

The project includes an `.editorconfig` file that your IDE should automatically detect. It configures:
- **Indentation:** 4 spaces for C#, 2-space tabs for YAML
- **Line endings:** LF (Unix-style)
- **C# style preferences:** var usage, modifier order, parentheses rules
- **ReSharper settings:** If using JetBrains Rider

### Formatting Guidelines

- **Braces:**
    - Use braces for all control structures except single-line `if` statements
    - Opening braces go on the next line for classes, methods, and properties
  - Leave empty line before and after code block (function, loop, if statement, ...)
- **Line Endings:** Use LF line endings
- **Namespaces:** Use file-scoped namespaces where possible
- **Extensions:** Use the newer extension syntax when available

### Error Handling

- **Use `IErrorCollector`:** For collecting and reporting multiple errors
- **Use `ILogger`:** From `Microsoft.Extensions.Logging` for logging
- **Return Patterns:** Methods that can fail should return `null` or use `IErrorCollector`
- **Exceptions:** Only throw in helper classes; avoid for expected error conditions
- **Validation:** Use the error collector pattern for validation failures

### Documentation

- **XML Comments:** Required for all public types and members
- **Private Members:** Add XML comments if not self-explanatory
- **Comment Style:** Explain *why*, not *what*
- **Examples:**
  ```csharp
  /// <summary>
  /// Validates that no two routers share the same name.
  /// </summary>
  /// <remarks>
  /// Router names must be unique to avoid configuration conflicts
  /// when generating vendor-specific outputs.
  /// </remarks>
  public void Validate()
  ```

### Naming Conventions

- **Classes, Methods, Properties:** `PascalCase`
- **Local Variables:** `camelCase` in methods
- **Class Fields:** `_camelCase` (with underscore prefix)
- **Interfaces:** Prefix with `I` (e.g., `IStep`, `IValidator`)
- **Keywords as Variables:** Use `@` prefix (e.g., `@interface`, `@class`)
- **Acronyms:** Treat as single words, except for Enum Members (e.g., `AsNumber` not `ASNumber`, but `iBGP` not `Ibgp`)

### Code Organization

- **Using Statements:**
    - Place at the top of the file, before namespace declaration
    - Sort alphabetically
    - Remove unused usings
    - Use aliases for clarity when needed: `using CustomName = Some.Long.Type;`
- **Types:**
    - Use `var` when the type is obvious from the right side
    - Use nullable reference types (`?`) appropriately
    - Leverage C# 14 features like `required` properties

## License

This project is licensed under the GNU General Public Licence v3.0 (GPL-3.0).
See the [LICENCE](LICENCE) file for details.

By contributing to router-quack, you agree that your contributions will be licensed under the same GPL-3.0 Licence.

## Use of AI

After experimenting with AI across different use cases,
I came to the conclusion that it can be an amazing tool... for **learning**.
In this project, I do not use AI to generate code, review code with no oversight or have any sort of responsibility.
I am fine with it double-checking my work, nothing more.

While this is my personal opinion, I do realise that I absolutely do not have a say on the way you should use AI.
If you choose to do use it in your contributions to router-qauck,
you will find a regurarly updated [AGENTS.md](AGENTS.md) file to make it as effective as possible.

## Questions?

If you have questions about the development process or need clarification on the architecture,
feel free to open a discussion issue with the `question` label.