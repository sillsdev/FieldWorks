---
applyTo: "**/*.cs, **/*.cpp, **/*.h"
name: "code-review.instructions"
description: "Code review guidelines for FieldWorks pull requests"
---

# Code Review Guidelines

## Purpose & Scope

This document provides guidance for code reviewers evaluating pull requests in the FieldWorks repository. It extracts key review principles to ensure code quality, security, and maintainability.

## What to Look For

### Licensing and Legal

- **New external assemblies/libraries/components**: Verify the license is compatible with LGPL 2.1+
- **Code copied from websites**: Check the license. Code without a license cannot be used (see [Infoworld article](http://www.infoworld.com/d/open-source-software/github-needs-take-open-source-seriously-208046))
- **License conditions**: Ensure any required disclaimers or attributions are included

### Exception Handling

- **Exceptions should be exceptional**: Don't use exceptions for normal control flow
- **Catch specific exceptions**: Avoid catching generic `Exception` without good reason
- **Clean up resources**: Ensure proper disposal in `finally` blocks or use `using` statements

### Code Quality

- **Follows coding standards**: See [coding-standard.instructions.md](coding-standard.instructions.md)
- **Clear naming**: Variables, methods, and classes have descriptive names
- **Appropriate comments**: Complex logic is explained; obvious code is not over-commented
- **No dead code**: Remove unused variables, methods, and commented-out code

### Testing

- **Tests included**: New functionality has corresponding tests
- **Tests pass**: All existing tests continue to pass
- **Edge cases covered**: Tests include boundary conditions and error cases

### Security

- **Input validation**: User input is validated before use
- **SQL injection prevention**: Parameterized queries for database access
- **Path traversal prevention**: File paths are validated and sanitized
- See [security.instructions.md](security.instructions.md) for comprehensive security guidelines

### Performance

- **No obvious performance issues**: Avoid N+1 queries, unnecessary loops, excessive allocations
- **Resource cleanup**: Disposable objects are properly disposed
- See [dispose.instructions.md](dispose.instructions.md) for IDisposable patterns

### Data Integrity

- **Migration safety**: Data model changes include proper migrations
- **Backward compatibility**: Consider upgrade paths for existing user data
- **Validation**: Data is validated before persistence

## Review Process

1. **Understand the context**: Read the PR description and linked issues
2. **Review the changes**: Look at each file systematically
3. **Run the code**: If significant, pull the branch and test locally
4. **Provide feedback**: Be specific, constructive, and kind
5. **Approve or request changes**: Be clear about blocking vs. non-blocking feedback

## Feedback Guidelines

### Be Constructive

- ✅ "Consider using `string.IsNullOrEmpty()` here for null safety"
- ❌ "This is wrong"

### Be Specific

- ✅ "Line 42: This could throw a NullReferenceException if `item` is null"
- ❌ "There might be bugs"

### Distinguish Blocking vs. Non-Blocking

- Use "nit:" or "suggestion:" prefix for non-blocking feedback
- Be explicit if something must be changed before approval

### Acknowledge Good Work

- Point out clever solutions or well-written code
- Thank contributors for their work

## References

- [managed.instructions.md](managed.instructions.md) - C# specific guidelines
- [native.instructions.md](native.instructions.md) - C++ specific guidelines
- [testing.instructions.md](testing.instructions.md) - Testing guidelines
