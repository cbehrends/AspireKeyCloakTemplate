# Error Handling Guidelines

## .NET
- Use try/catch for recoverable errors
- Throw custom exceptions for domain-specific errors
- Log errors using the configured logger
- Return appropriate HTTP status codes and error messages

## Frontend
- Handle API errors gracefully
- Show user-friendly error messages

## BFF
- Map backend errors to frontend-friendly responses
- Log and track errors for diagnostics

