# Contributing to Azure Service Bus POC

Thank you for your interest in contributing to this Azure Service Bus POC project! This document provides guidelines for contributing to the project.

## Development Setup

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Azure Service Bus namespace (or use the provided test connection)

### Getting Started
1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature/fix
4. Make your changes
5. Test your changes thoroughly
6. Submit a pull request

## Code Style Guidelines

### C# Coding Standards
- Use PascalCase for public members
- Use camelCase for private members
- Use meaningful variable and method names
- Include XML documentation for public APIs
- Follow .NET coding conventions

### Project Structure
- Keep shared models in `ServiceBus.Shared`
- Maintain separation between WCF service and consumer service
- Add comprehensive tests for new features

## Testing Requirements

### Before Submitting
1. Run all existing tests: `dotnet test`
2. Test the complete end-to-end flow: `CompleteEndToEndTest`
3. Verify both services start correctly
4. Test with both valid and invalid messages

### Test Coverage
- Unit tests for business logic
- Integration tests for Service Bus operations
- End-to-end tests for complete workflows

## Pull Request Process

1. **Create a descriptive title** for your pull request
2. **Provide detailed description** of changes made
3. **Reference any related issues** using `#issue-number`
4. **Include test results** showing your changes work
5. **Update documentation** if needed

### PR Checklist
- [ ] Code follows project style guidelines
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated (if applicable)
- [ ] No breaking changes (or clearly documented)

## Reporting Issues

### Bug Reports
When reporting bugs, please include:
- **Environment details** (.NET version, OS, etc.)
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Log outputs** or error messages
- **Configuration details** (sanitized connection strings)

### Feature Requests
For new features, please provide:
- **Clear description** of the proposed feature
- **Use case scenarios** where it would be helpful
- **Proposed implementation approach** (if you have ideas)

## Architecture Guidelines

### Adding New Message Types
1. Create model in `ServiceBus.Shared/Models`
2. Update `ServiceBusPublisher` interface if needed
3. Add consumer logic in `ERP.ConsumerService`
4. Include comprehensive tests

### Service Modifications
- Maintain backward compatibility when possible
- Update health checks for new dependencies
- Ensure proper error handling and logging
- Update configuration documentation

## Security Considerations

### Connection Strings
- Never commit real connection strings
- Use placeholder values in configuration files
- Document environment variable alternatives
- Consider using Azure Key Vault for production

### Data Privacy
- Sanitize logs of sensitive information
- Follow least-privilege principles
- Validate all input data
- Use secure communication protocols

## Documentation Standards

### Code Documentation
- XML comments for public APIs
- Inline comments for complex logic
- Clear variable and method names
- Architecture decision records for major changes

### README Updates
- Keep installation instructions current
- Update configuration examples
- Include new testing scenarios
- Document breaking changes

## Community Guidelines

### Communication
- Be respectful and professional
- Provide constructive feedback
- Help newcomers get started
- Share knowledge and best practices

### Code of Conduct
- Follow the project's code of conduct
- Report inappropriate behavior
- Foster an inclusive environment
- Encourage collaboration

## Getting Help

### Resources
- Check existing issues and documentation first
- Use GitHub Discussions for questions
- Reference Azure Service Bus documentation
- Review .NET and CoreWCF documentation

### Contact
- Create GitHub issues for bugs and features
- Use GitHub Discussions for general questions
- Tag maintainers for urgent issues

Thank you for contributing to make this project better! 🚀
