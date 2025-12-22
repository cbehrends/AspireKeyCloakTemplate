## Plan: Reliable Integration Test Execution (Local & GitHub)

This plan aims to ensure all integration tests run reliably both locally and in GitHub Actions. The likely root cause is path resolution in `AppHost.cs` (e.g., for config files), which may differ between local/dev and CI environments. The plan covers diagnosing, fixing, and validating test execution in both contexts.

### Steps
1. Review current GitHub Actions workflow(s) for test execution setup and environment.
2. Analyze `AppHost.cs` for any hardcoded or relative paths, especially for config files.
3. Identify how integration tests launch AspireHost and what paths/configs they depend on.
4. Refactor path handling in `AppHost.cs` to be robust to different working directories (e.g., use `AppContext.BaseDirectory` or environment variables).
5. Update integration test setup to ensure correct working directory and environment in both local and CI runs.
6. Validate by running integration tests locally and in GitHub Actions, confirming all pass.

### Further Considerations
1. Should config files be copied to output/test directories, or should code resolve paths dynamically?
2. Is there a need for test-specific appsettings or secrets handling in CI?
3. Would a test utility/helper for path resolution improve maintainability?

