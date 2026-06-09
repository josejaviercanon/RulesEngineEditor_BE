## ADDED Requirements

### Requirement: Structural deep comparison between RuleResultTree and ExpectedOutputJson
The system SHALL provide a `Verify` method that accepts a `RuleResultTree` and an expected output JSON string, and SHALL perform a structural deep comparison returning pass/fail status with detailed diffs.

#### Scenario: Exact structural match
- **WHEN** the `RuleResultTree` output matches the `ExpectedOutputJson` structure exactly (same keys, same values, same nesting)
- **THEN** the `Verify` method returns `IsMatch = true` with an empty differences collection

#### Scenario: Value mismatch in a nested property
- **WHEN** the `RuleResultTree` contains `{ "result": { "score": 85 } }` and the `ExpectedOutputJson` contains `{ "result": { "score": 90 } }`
- **THEN** the `Verify` method returns `IsMatch = false` with a difference entry indicating the path `result.score` has actual `85` vs expected `90`

#### Scenario: Missing expected property in actual output
- **WHEN** the `RuleResultTree` contains `{ "status": "success" }` and the `ExpectedOutputJson` contains `{ "status": "success", "message": "done" }`
- **THEN** the `Verify` method returns `IsMatch = false` with a difference entry indicating the path `message` is missing from the actual output

#### Scenario: Extra property in actual output
- **WHEN** the `RuleResultTree` contains `{ "status": "success", "timestamp": "2026-01-01" }` and the `ExpectedOutputJson` contains `{ "status": "success" }`
- **THEN** the `Verify` method returns `IsMatch = true` by default (extra properties in actual output are ignored) OR `IsMatch = false` if strict mode is enabled

### Requirement: Verify returns structured difference information
The system SHALL return differences as a collection of structured objects, each containing the JSON path, expected value, actual value, and a human-readable message.

#### Scenario: Difference object structure
- **WHEN** a comparison finds a mismatch at `result.items[0].name`
- **THEN** the difference object contains `Path = "result.items[0].name"`, `Expected = "Widget"`, `Actual = "Gadget"`, and `Message = "Value mismatch at 'result.items[0].name': expected 'Widget' but got 'Gadget'"`

### Requirement: Verify handles null inputs gracefully
The system SHALL handle null or empty inputs to the `Verify` method without throwing exceptions, returning appropriate error results.

#### Scenario: Null RuleResultTree
- **WHEN** `Verify` is called with a null `RuleResultTree`
- **THEN** the method returns `IsMatch = false` with a difference message indicating the actual result is null

#### Scenario: Null ExpectedOutputJson
- **WHEN** `Verify` is called with a null or empty `ExpectedOutputJson`
- **THEN** the method returns `IsMatch = false` with a difference message indicating the expected output is null or empty

#### Scenario: Both inputs null
- **WHEN** `Verify` is called with both inputs null
- **THEN** the method returns `IsMatch = true` (both are null, which matches)

### Requirement: Verify normalizes RuleResultTree to JSON for comparison
The system SHALL convert the `RuleResultTree` to a JSON representation (using Newtonsoft.Json or System.Text.Json) before comparing it to the `ExpectedOutputJson`, ensuring a consistent format for structural comparison.

#### Scenario: RuleResultTree converted to JSON
- **WHEN** `Verify` is called with a `RuleResultTree` containing successful rule evaluations
- **THEN** the `RuleResultTree` is serialized to a JSON structure before being compared to the parsed `ExpectedOutputJson`

#### Scenario: Serialization handles RuleResultTree hierarchy
- **WHEN** the `RuleResultTree` contains nested child result trees
- **THEN** the serialized JSON preserves the nested hierarchy (children appear under a `ChildResults` array)
