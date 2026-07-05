# 02-final-validation: Validate the upgraded solution

Verify the upgraded solution end-to-end. Perform a clean full-solution build, run any existing test suite, and review the behavioral-change APIs the assessment flagged so nothing regresses at runtime.

The assessment identified 183 behavioral changes concentrated in `System.Net.Http.HttpContent`, `System.Text.Json.JsonDocument`, and `System.Uri`. These require no code changes to compile but should be exercised (e.g., API request/response handling in KronoxApi, `HttpClient` calls and JSON handling in KronoxFront) to confirm behavior is unchanged on .NET 10. Record any recommendation that is intentionally deferred.

**Done when**: the solution builds clean (0 errors, 0 warnings); all discovered tests pass (or it is confirmed there are no test projects); and any deferred behavioral-change notes are recorded in this task's progress.
