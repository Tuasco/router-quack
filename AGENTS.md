# AGENTS.md

This document provides guidelines for AI agents working on this codebase.

## 1. Project Overview

This is a .NET project that generates router configurations from YAML intent files. The project is structured into three main parts:

- `RouterQuack.CLI`: The command-line interface for the tool.
- `RouterQuack.Core`: The core logic of the application.
- `RouterQuack.IO.Yaml`: Handles parsing of YAML files.

The project is written in C# 14 and targets the .NET 10.0 framework.

## 2. Build, Lint, and Test

### Build

To build the project, run the following command from the root directory:

```bash
dotnet build
```

### Lint

There is no specific linting tool configured for this project. However, the project has nullable reference types enabled, so the C# compiler will perform some static analysis. Pay attention to any warnings related to nullability.

### Test

There are currently no tests in this project.

## 3. Code Style Guidelines

### Formatting

- **Indentation**: Use 4 spaces for indentation.
- **Braces**: Use braces for all control structures, but not single-line `if` statements. Opening braces for classes,
  methods, and properties should be on the next line.
- **Line Endings**: Use LF line endings.
- **File-scoped namespaces**: Use file-scoped namespaces where possible.
- **Extension**: Use the newer extension syntax when possible.

### Naming Conventions

- **Classes, Methods, and Properties**: Use `PascalCase`.
- **Local Variables**: Use `camelCase` in functions and `prepended camelCase` in classes (e.g., _myVariable).
- **Interfaces**: Prefix with `I` (e.g., `IStep`).
- **Keywords**: Use the `@` prefix for variables that are also C# keywords (e.g., `@interface`).
- **Acronyms**: Treat acronyms as single words in `PascalCase` and `camelCase` (e.g., `AsNumber` not `ASNumber`).

### Imports

- `using` statements should be placed at the top of the file, before the namespace declaration.
- Sort `using` statements alphabetically.
- Remove unused `using` statements.
- To avoid type confusion, use `using CustomTypeName = ActualType;`.

### Types

- The project uses C# 14 features like `required` properties.
- Nullable reference types are enabled, so use `?` to denote nullable types.
- Use `var` when the type is obvious from the right-hand side of the assignment.

### Error Handling

- Use the `IErrorCollector` interface to collect and log errors.
- Use the `ILogger` interface from `Microsoft.Extensions.Logging` for logging.
- Methods that can fail should return `null` or use the `IErrorCollector` to record errors. Only throw errors if the
  method is in a helper class.
- Avoid throwing exceptions for expected error conditions. Instead, use the error collector and return a `null` or a result object.

### Comments

- Add XML documentation comments to all public types and members.
- Add XML documentation comments to private members that are not self-explanatory.
- Use comments to explain *why* something is done, not *what* is being done.

## 4. Architectural Patterns

- **Dependency Injection**: The project uses dependency injection, configured in `src/RouterQuack.CLI/Startup/DependencyInjection.cs`. When adding new services, register them there.
- **Steps**: The core logic is broken down into a series of "steps" that implement the `IStep` interface. When adding new processing stages, create a new step and add it to the execution pipeline.

## 5. Git Workflow

- **Branching**: Create a new branch for each new feature or bug fix.
- **Commits**: Write clear and concise commit messages. Reference issue numbers if applicable.
- **Pull Requests**: Create a pull request to merge your changes into the `main` branch. Ensure that the build passes before merging.
