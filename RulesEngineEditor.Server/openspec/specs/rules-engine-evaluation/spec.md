# Rules Engine Evaluation

## Purpose

The Rules Engine Evaluation service provides stateless per-request evaluation of Microsoft RulesEngine workflows. It handles dynamic workflow JSON, nested workflows, nested rule hierarchies, rule actions, LINQ expressions, CustomTypes registration, and ReSettings configuration.

## Requirements

### Requirement: Stateless evaluation per request
The system SHALL instantiate a new `Microsoft.RulesEngine.RulesEngine` instance for each evaluation request using the provided workflow JSON and `ReSettings`. The engine instance MUST NOT be cached, reused across requests, or registered as a singleton in the DI container.

#### Scenario: Fresh engine per evaluation call
- **WHEN** two concurrent requests invoke the evaluation service with different workflow JSON payloads
- **THEN** each request creates its own `RulesEngine` instance and evaluates independently without interfering with the other

#### Scenario: No global rule registration
- **WHEN** the application starts up
- **THEN** no workflow definitions are pre-loaded or registered globally with the RulesEngine library

### Requirement: Support Workflows and Nested Workflows
The system SHALL accept workflow JSON containing both top-level workflows and nested (child) workflows, and SHALL execute them according to the RulesEngine nested workflow execution mode specified in `ReSettings`.

#### Scenario: Execute workflow with nested child workflows
- **WHEN** a workflow JSON document contains a parent workflow that references child workflows by name
- **THEN** the evaluation service passes all workflows to the `RulesEngine` constructor and the engine resolves nested workflow references during evaluation

#### Scenario: Configure nested workflow execution mode
- **WHEN** `ReSettings` specifies `NestedRuleExecutionMode` as `All`
- **THEN** the engine evaluates all matching rules including those in nested workflows

### Requirement: Support Nested Rule Hierarchies
The system SHALL support rule definitions with nested child rules (rules within rules) and SHALL evaluate them according to the parent rule's operator (And/Or).

#### Scenario: Evaluate nested rules with And operator
- **WHEN** a rule contains three nested child rules and the parent operator is `And`
- **THEN** the parent rule evaluates to `True` only if all three child rules succeed

#### Scenario: Evaluate nested rules with Or operator
- **WHEN** a rule contains three nested child rules and the parent operator is `Or`
- **THEN** the parent rule evaluates to `True` if at least one child rule succeeds

### Requirement: Support Rule Actions (Success/Failure)
The system SHALL support `OnSuccess` and `OnFailure` action blocks in rule definitions and SHALL execute them when rule conditions are met.

#### Scenario: Execute OnSuccess action
- **WHEN** a rule's expression evaluates to `True` and the rule has an `OnSuccess` action defined
- **THEN** the action is executed and its output is included in the `RuleResultTree`

#### Scenario: Execute OnFailure action
- **WHEN** a rule's expression evaluates to `False` and the rule has an `OnFailure` action defined
- **THEN** the action is executed and its output is included in the `RuleResultTree`

### Requirement: Support LINQ-based Expressions
The system SHALL evaluate rule expressions written in LINQ syntax (e.g., `input1.Country == "USA"`, `input1.Age > 21`) against the provided facts.

#### Scenario: Evaluate a simple LINQ expression
- **WHEN** a rule's expression is `input1.Amount > 1000` and the facts contain `Amount = 1500`
- **THEN** the rule evaluates to `True`

#### Scenario: Evaluate a complex LINQ expression
- **WHEN** a rule's expression is `input1.Amount > 1000 AND input1.Country == "USA"` and the facts contain `Amount = 1500, Country = "Canada"`
- **THEN** the rule evaluates to `False`

### Requirement: Expose RuleResultTree for inspection
The system SHALL return the full `RuleResultTree` from each evaluation, including per-rule success/failure status, exception messages, action outputs, and child result trees.

#### Scenario: Inspect RuleResultTree after evaluation
- **WHEN** a workflow evaluation completes
- **THEN** the response includes a structured representation of the `RuleResultTree` with `RuleName`, `IsSuccess`, `ActionResult.Output`, `ExceptionMessage`, and `ChildResults` for each evaluated rule

### Requirement: Dynamic CustomTypes registration
The system SHALL accept an array of assembly-qualified type names in the evaluation request and SHALL resolve them via reflection, adding valid resolved types to `ReSettings.CustomTypes` before engine instantiation.

#### Scenario: Register a custom type for use in expressions
- **WHEN** the request includes `CustomTypes: ["RulesEngineEditor.Server.Business.Entities.Models.Customer, RulesEngineEditor.Server"]` and the type exists
- **THEN** the type is resolved and added to `ReSettings.CustomTypes`, allowing rule expressions to reference properties of that type

#### Scenario: Custom type does not exist
- **WHEN** the request includes a `CustomTypes` entry for a type that cannot be resolved
- **THEN** the system returns a 400 Bad Request error with a message identifying the unresolvable type

#### Scenario: Custom type is a value type
- **WHEN** the request includes a `CustomTypes` entry that resolves to `System.Int32`
- **THEN** the system returns a 400 Bad Request error rejecting the value type (only reference types are supported)

### Requirement: ReSettings configuration support
The system SHALL accept `ReSettings` configuration in the evaluation request, including `CustomTypes`, `EnableScopedParams`, `NestedRuleExecutionMode`, and other supported properties, and SHALL pass them to the `RulesEngine` constructor.

#### Scenario: Configure all ReSettings options
- **WHEN** the request includes `SettingsJson: { "EnableScopedParams": true, "NestedRuleExecutionMode": "All", "EnableExceptionAsErrorMessage": false }`
- **THEN** the `RulesEngine` is instantiated with those settings applied

#### Scenario: Default ReSettings when not provided
- **WHEN** the request does NOT include `SettingsJson` or includes an empty object
- **THEN** the system uses the RulesEngine library defaults (which validate rules but allow lenient settings)

### Requirement: Validate workflow JSON before evaluation
The system SHALL validate the provided workflow JSON structure before attempting to instantiate the `RulesEngine`, catching malformed JSON early with clear error messages.

#### Scenario: Malformed workflow JSON
- **WHEN** the request includes `RulesJson` that is not valid JSON (e.g., missing closing brace)
- **THEN** the system returns a 400 Bad Request with a parse error message identifying the JSON issue

#### Scenario: Invalid workflow structure
- **WHEN** the request includes valid JSON that does not conform to the RulesEngine workflow schema
- **THEN** the system returns a 400 Bad Request with the `RuleValidationException` details
