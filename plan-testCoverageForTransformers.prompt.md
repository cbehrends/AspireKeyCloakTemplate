## Plan: Test Coverage for Transformers

Establish comprehensive unit and integration test coverage for all transformers in `src/DotNetCleanTemplate.Gateway/Features/Transformers`. This ensures each transformer’s logic is validated in isolation (unit) and within the application pipeline (integration).

### Steps
1. Review each transformer’s responsibilities and public API in the `Transformers` folder.
2. Design unit test cases for each transformer in `DotNetCleanTemplate.Gateway.UnitTests/Features/Transformers/`.
3. Design integration test scenarios for each transformer in `DotnetCleanTemplate.Gateway.IntegrationTests/`.
4. Create or update test classes and methods to cover normal, edge, and error cases.
5. Ensure tests are discoverable and runnable via the solution’s test runner.
6. Integration tests should use Aspire framework to simulate real-world usage.

### Further Considerations
1. Should integration tests use in-memory hosting or real HTTP calls?
2. Are there any external dependencies (e.g., authentication, tokens) that need to be mocked or configured?
3. Confirm naming conventions for test classes and methods for consistency.
4. Tests should use NSubstitute for mocking dependencies where applicable.
5. Tests should validate using Shouldly for assertions.
