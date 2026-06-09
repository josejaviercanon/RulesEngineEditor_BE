using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulesEngine.Exceptions;
using RulesEngine.Models;

namespace RulesEngineEditor.Server.Business.Services;

public sealed class RulesEvaluationService : IRulesEvaluationService
{
    public async Task<EvaluationResult> EvaluateAsync(
        string rulesJson,
        string factsJson,
        string? settingsJson,
        string[]? customTypes,
        CancellationToken ct = default)
    {
        try
        {
            // Parse workflow JSON
            Workflow[]? workflows;
            try
            {
                workflows = JsonConvert.DeserializeObject<Workflow[]>(rulesJson);
            }
            catch (JsonException ex)
            {
                return new EvaluationResult(false, null, $"Invalid RulesJson: {ex.Message}");
            }

            if (workflows is null or { Length: 0 })
            {
                return new EvaluationResult(false, null, "RulesJson must contain at least one workflow definition.");
            }

            // Parse facts into RuleParameter array
            RuleParameter[] ruleParameters;
            try
            {
                var facts = JsonConvert.DeserializeObject<Dictionary<string, object>>(factsJson);
                if (facts is null or { Count: 0 })
                {
                    return new EvaluationResult(false, null, "FactsJson must contain at least one fact.");
                }

                ruleParameters = facts
                    .Select(kvp => new RuleParameter(kvp.Key, kvp.Value))
                    .ToArray();
            }
            catch (JsonException ex)
            {
                return new EvaluationResult(false, null, $"Invalid FactsJson: {ex.Message}");
            }

            // Parse settings
            ReSettings reSettings = new();
            if (!string.IsNullOrWhiteSpace(settingsJson))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ReSettings>(settingsJson);
                    if (parsed is not null)
                    {
                        reSettings = parsed;
                    }
                }
                catch (JsonException ex)
                {
                    return new EvaluationResult(false, null, $"Invalid SettingsJson: {ex.Message}");
                }
            }

            // Resolve custom types
            if (customTypes is { Length: > 0 })
            {
                var resolvedTypes = new List<Type>();
                foreach (var typeName in customTypes)
                {
                    var resolved = Type.GetType(typeName);

                    // Fallback: try loading the assembly by name from the type's assembly-qualified name
                    if (resolved is null)
                    {
                        var commaIndex = typeName.IndexOf(',');
                        if (commaIndex > 0)
                        {
                            var assemblyName = typeName[(commaIndex + 1)..].Trim();
                            try
                            {
                                var assembly = Assembly.Load(assemblyName);
                                resolved = assembly.GetType(typeName[..commaIndex].Trim());
                            }
                            catch
                            {
                                // Assembly load failed; continue to scan fallback
                            }
                        }
                    }

                    // Final fallback: scan all loaded assemblies
                    resolved ??= AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a =>
                        {
                            try { return a.GetTypes(); }
                            catch { return Type.EmptyTypes; }
                        })
                        .FirstOrDefault(t => t.FullName == typeName || t.AssemblyQualifiedName == typeName);

                    if (resolved is null)
                    {
                        return new EvaluationResult(false, null, $"Custom type '{typeName}' could not be resolved.");
                    }

                    if (resolved.IsValueType)
                    {
                        return new EvaluationResult(false, null, $"Custom type '{typeName}' is a value type. Only reference types (classes) are supported.");
                    }

                    resolvedTypes.Add(resolved);
                }

                reSettings.CustomTypes = resolvedTypes.ToArray();
            }

            // Instantiate fresh RulesEngine per request
            var engine = new RulesEngine.RulesEngine(workflows, reSettings);

            // Execute all rules for the first workflow
            var workflowName = workflows[0].WorkflowName;
            var resultTree = await engine.ExecuteAllRulesAsync(workflowName, ruleParameters);

            return new EvaluationResult(true, resultTree.ToList(), null);
        }
        catch (RuleValidationException ex)
        {
            return new EvaluationResult(false, null, $"Workflow validation failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
        {
            return new EvaluationResult(false, null, $"Evaluation error: {ex.Message}");
        }
    }

    public VerificationResult Verify(RuleResultTree resultTree, string? expectedOutputJson)
    {
        // Handle null cases
        if (resultTree is null && expectedOutputJson is null)
        {
            return new VerificationResult(true, Array.Empty<Difference>());
        }

        if (resultTree is null)
        {
            return new VerificationResult(false, new[]
            {
                new Difference("$", expectedOutputJson, null, "Actual RuleResultTree is null but expected output was provided.")
            });
        }

        if (string.IsNullOrWhiteSpace(expectedOutputJson))
        {
            return new VerificationResult(false, new[]
            {
                new Difference("$", null, "RuleResultTree present", "ExpectedOutputJson is null or empty but a RuleResultTree was produced.")
            });
        }

        // Serialize RuleResultTree to JSON
        JToken actualToken;
        try
        {
            var actualJson = JsonConvert.SerializeObject(resultTree, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Include
            });
            actualToken = JToken.Parse(actualJson);
        }
        catch (JsonException ex)
        {
            return new VerificationResult(false, new[]
            {
                new Difference("$", expectedOutputJson, null, $"Failed to serialize RuleResultTree: {ex.Message}")
            });
        }

        // Parse expected JSON
        JToken expectedToken;
        try
        {
            expectedToken = JToken.Parse(expectedOutputJson);
        }
        catch (JsonException ex)
        {
            return new VerificationResult(false, new[]
            {
                new Difference("$", expectedOutputJson, null, $"Failed to parse ExpectedOutputJson: {ex.Message}")
            });
        }

        // Perform deep comparison
        var differences = new List<Difference>();
        DeepCompare(actualToken, expectedToken, "$", differences);

        return new VerificationResult(differences.Count == 0, differences);
    }

    private static void DeepCompare(JToken actual, JToken expected, string path, List<Difference> differences)
    {
        if (actual.Type != expected.Type)
        {
            differences.Add(new Difference(path, expected.ToString(Formatting.None), actual.ToString(Formatting.None),
                $"Type mismatch at '{path}': expected {expected.Type} but got {actual.Type}"));
            return;
        }

        switch (actual.Type)
        {
            case JTokenType.Object:
                CompareObjects((JObject)actual, (JObject)expected, path, differences);
                break;

            case JTokenType.Array:
                CompareArrays((JArray)actual, (JArray)expected, path, differences);
                break;

            default:
                if (!JToken.DeepEquals(actual, expected))
                {
                    differences.Add(new Difference(path, expected.ToString(Formatting.None), actual.ToString(Formatting.None),
                        $"Value mismatch at '{path}': expected '{expected}' but got '{actual}'"));
                }
                break;
        }
    }

    private static void CompareObjects(JObject actual, JObject expected, string path, List<Difference> differences)
    {
        foreach (var expectedProp in expected.Properties())
        {
            var propPath = string.IsNullOrEmpty(path) ? expectedProp.Name : $"{path}.{expectedProp.Name}";

            if (!actual.TryGetValue(expectedProp.Name, out var actualValue))
            {
                differences.Add(new Difference(propPath, expectedProp.Value.ToString(Formatting.None), null,
                    $"Missing property '{propPath}' in actual output"));
            }
            else
            {
                DeepCompare(actualValue!, expectedProp.Value, propPath, differences);
            }
        }
    }

    private static void CompareArrays(JArray actual, JArray expected, string path, List<Difference> differences)
    {
        if (actual.Count != expected.Count)
        {
            differences.Add(new Difference(path, $"array[{expected.Count}]", $"array[{actual.Count}]",
                $"Array length mismatch at '{path}': expected {expected.Count} items but got {actual.Count}"));
            return;
        }

        for (var i = 0; i < actual.Count; i++)
        {
            var itemPath = $"{path}[{i}]";
            DeepCompare(actual[i], expected[i], itemPath, differences);
        }
    }
}
