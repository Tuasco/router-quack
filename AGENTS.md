# AGENTS.md

This document provides guidelines for AI agents working on this codebase.

## 1. Project Overview

This is a .NET project that generates router configurations from YAML intent files. The project is structured into four
main parts:
- `RouterQuack.CLI`: The command-line interface for the tool.
- `RouterQuack.Core`: The core logic of the application.
- `RouterQuack.IO.Yaml`: Handles parsing of YAML files.
- `RouterQuack.IO.Cisco`: Handles output of Cisco IOS configurations.

The project is written in C# 14 and targets the .NET 10.0 framework.

## 2. Build, Lint, and Test

### Build

To build the project, run the following command from the root directory:

```bash
dotnet build
```

### Lint

There is no specific linting tool configured for this project. The project uses:

- **Nullable reference types** enabled - pay attention to nullability warnings.
- **Implicit usings** enabled - avoid redundant using statements.
- **ReSharper** settings in `.editorconfig` for formatting rules.

### Test

To run all tests:

```bash
dotnet test
```

The project uses **TUnit** as the testing framework with **NSubstitute** for mocking.
Tests are located in the `tests/RouterQuack.Tests/` directory.

## 3. Project Structure

```
src/
├── RouterQuack.CLI/          # Command-line entry point
│   ├── Startup/
│   │   ├── DependencyInjection.cs   # DI container configuration
│   │   └── ArgumentsParser.cs      # CLI argument parsing
│   └── Program.cs
├── RouterQuack.Core/          # Core domain logic
│   ├── Models/                # Domain models (Context, Router, As, Interface, Address)
│   ├── Validators/            # Validation steps (implement IValidator)
│   ├── Processors/            # Processing steps (implement IProcessor)
│   ├── Extensions/            # Extension methods for IStep, collections
│   ├── ConfigFileWriters/     # Output writers (implement IConfigFileWriter)
│   ├── IntentFileParsers/     # Input parsers (implement IIntentFileParser)
│   └── Utils/                 # Utility classes (NetworkUtils, RouterUtils)
├── RouterQuack.IO.Yaml/       # YAML parsing
│   ├── Models/                # YAML-specific models
│   └── Parser/                # Mappers from YAML to Core models
└── RouterQuack.IO.Cisco/      # Cisco configuration output
    └── Utils/                 # Configuration section generators

tests/RouterQuack.Tests/
└── Unit/                      # Unit tests mirroring src structure
    └── TestHelpers/           # Test utilities (ContextFactory, TestData)
```

## 4. Code Style Guidelines

### Formatting

- **Indentation**: Use 4 spaces for indentation (enforced by `.editorconfig`).
- **Line Endings**: Use LF line endings.
- **Braces**: Opening braces for classes, methods, and properties should be on the next line (Allman style). Use braces
  for all control structures.
- **File-scoped namespaces**: Use file-scoped namespaces where possible.
- **Extension methods**: Use the newer extension syntax when possible.
- **Object initialisers**: `csharp_new_line_before_members_in_object_initializers = false` (single line allowed).
- **Wrap long object/collection initialisers**: `wrap_if_long`.

### Naming Conventions

| Element                      | Convention      | Example                                                   |
|------------------------------|-----------------|-----------------------------------------------------------|
| Classes, Methods, Properties | `PascalCase`    | `ValidNetworkSpaces`, `Validate()`                        |
| Local Variables              | `camelCase`     | `v4Space`, `logger`                                       |
| Private Fields               | `_camelCase`    | `_logger`, `_context`                                     |
| Interfaces                   | Prefix with `I` | `IStep`, `IValidator`, `IProcessor`                       |
| Parameters                   | `camelCase`     | `string message`                                          |
| Keywords                     | Use `@` prefix  | `@interface`, `@event`                                    |
| Acronyms                     | Single word     | `AsNumber`, `XmlDocument` (not `ASNumber`, `XMLDocument`) |

### Imports

- `using` statements should be placed at the top of the file, before the namespace declaration.
- Sort `using` statements alphabetically.
- Remove unused `using` statements.
- Use type aliases to avoid confusion with `using CustomTypeName = ActualType;`
    - Example in `GlobalUsings.cs`: `using YamlAs = RouterQuack.IO.Yaml.Models.As;`

### Types

- The project uses C# 14 features like `required` properties and collection expressions (`[]`).
- Nullable reference types are enabled - use `?` to denote nullable types.
- Use `var` when the type is obvious from the right-hand side.
- Prefer predefined types: `var` for built-in types when type is not evident.
- Use target-typed object creation where the type is not obvious.

### Comments

- Add XML documentation comments (`/// <summary>...</summary>`) to all public types and members.
- Add XML documentation comments to private members that are not self-explanatory.
- Use comments to explain *why* something is done, not *what* is being done.
- Use ReSharper tags like `[StructuredMessageTemplate]` for logging parameters.

## 5. Error Handling

- Use the `IErrorCollector` interface (via `Context`) to collect and log errors.
- Use the `ILogger` interface from `Microsoft.Extensions.Logging` for logging.
- Methods that can fail should return `null` or use the `Context` to record errors.
- Only throw exceptions if the method is in a helper class or for truly exceptional cases.
- Avoid throwing exceptions for expected error conditions.
- Use structured logging with message templates: `Logger.LogError("Router {RouterName} not found", name)`

## 6. Architectural Patterns

### Dependency Injection

The project uses dependency injection, configured in `src/RouterQuack.CLI/Startup/DependencyInjection.cs`. When adding
new services:

- Register validators with keyed singleton: `AddKeyedSingleton<IValidator, YourValidator>(nameof(YourValidator))`
- Register processors with keyed singleton: `AddKeyedSingleton<IProcessor, YourProcessor>(nameof(YourProcessor))`
- Register parsers/writers as standard singletons.

### Steps Pattern

The core logic is broken down into a series of "steps" that implement interfaces:

| Interface           | Purpose                | Example                     |
|---------------------|------------------------|-----------------------------|
| `IIntentFileParser` | Read input files       | `YamlParser`                |
| `IValidator`        | Validate configuration | `ValidNetworkSpaces`        |
| `IProcessor`        | Process/modify data    | `GenerateLoopbackAddresses` |
| `IConfigFileWriter` | Write output files     | `CiscoWriter`               |

When adding new steps:

1. Create a class implementing the appropriate interface
2. Add XML documentation with `<summary>` and parameter docs
3. Register in `DependencyInjection.cs`
4. Add unit tests in `tests/RouterQuack.Tests/`

### Step Logging Extensions

Use extension methods from `StepExtensions` for consistent logging:

```csharp
source.Log(router, "Router-specific message", LogLevel.Warning);
source.Log(@interface, "Interface-specific message");
source.Log(as, "AS-specific message");
source.LogError("General error message {Arg}", arg);
source.LogWarning("Warning message");
```

## 7. Testing Guidelines

### Test Naming

Follow the pattern: `Method_Scenario_ExpectedResult`

```csharp
[Test]
public async Task Validate_ValidSpaces_NoErrors()
{
    // Arrange
    // Act
    // Assert
}
```

### Test Structure

Use the **Arrange-Act-Assert** pattern:

```csharp
[Test]
[Arguments("10.0.0.0/8", "2001:db8::/32")]
public async Task Validate_ValidSpaces_NoErrors(string v4Space, string v6Space)
{
    // Arrange
    var asses = new List<As> { /* ... */ };
    var context = ContextFactory.Create(asses: asses);
    var validator = new ValidNetworkSpaces(_logger, context);

    // Act
    validator.Validate();

    // Assert
    await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
}
```

### Test Helpers

Use `ContextFactory` and `TestData` from `tests/RouterQuack.Tests/Unit/TestHelpers/`:

```csharp
var context = ContextFactory.Create(asses: myAsList);
var router = TestData.CreateRouter();
var as = TestData.CreateAs(routers: [router], networksSpaceV4: IPNetwork.Parse("10.0.0.0/8"));
```

### Mocking

Use NSubstitute for mocking:

```csharp
private readonly ILogger<YourClass> _logger = Substitute.For<ILogger<YourClass>>();
_substitute.Received().SomeMethod();
```

## 8. Git Workflow

- **Branching**: Create a new branch for each new feature or bug fix.
- **Commits**: Write clear and concise commit messages. Reference issue numbers if applicable.
- **Pull Requests**: Create a pull request to merge changes into the `main` branch. Ensure the build passes.
- Never commit secrets, credentials, or sensitive configuration files.
