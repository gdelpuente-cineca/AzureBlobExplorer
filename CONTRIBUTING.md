# Contributing to Azure Blob Explorer

Thank you for your interest in contributing!

## Code of Conduct

Be respectful, inclusive, and professional in all interactions.

## How to Contribute

### Reporting Bugs

1. Check existing issues to avoid duplicates
2. Use the bug report template
3. Include:
   - Clear description
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment (OS, .NET version, etc)
   - Logs if applicable

### Suggesting Features

1. Use the feature request template
2. Explain the use case
3. Describe expected behavior
4. Consider performance/scalability impact

### Submitting Pull Requests

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit with clear messages: `git commit -m "feat: add new feature"`
4. Push to your fork: `git push origin feature/your-feature`
5. Open a Pull Request with description

### Code Style

- Follow C# conventions (PascalCase for classes, camelCase for variables)
- Use meaningful variable/method names
- Add XML comments for public APIs
- Keep methods focused and small
- Write async/await style code
- Add unit tests for new features

### Testing

```bash
# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Commit Messages

Follow conventional commits:
```
feat: add user access policy management
fix: resolve token expiration issue
docs: update deployment guide
refactor: simplify blob service
test: add unit tests for auth service
```

## Development Setup

```bash
# Clone and setup
git clone https://github.com/gdelpuente-cineca/AzureBlobExplorer.git
cd AzureBlobExplorer
dotnet restore

# Configure local secrets
dotnet user-secrets init
dotnet user-secrets set "AzureAdB2C:ClientSecret" "your-secret"
dotnet user-secrets set "AzureStorage:ConnectionString" "your-connection-string"

# Run
dotnet run
```

## Release Process

1. Update version in `.csproj`
2. Update CHANGELOG.md
3. Create git tag: `git tag v1.2.0`
4. Push tag: `git push origin v1.2.0`
5. Build release: Docker image and GitHub release

## Questions?

Open an issue or discussion!
