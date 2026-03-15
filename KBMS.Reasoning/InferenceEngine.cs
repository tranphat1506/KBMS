using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using KBMS.Models;
using NCalc; // We will need NCalc or a similar math evaluator for parsing formulas

namespace KBMS.Reasoning;

/// <summary>
/// The core Reasoning Engine implementing COKB solving methodologies (FClosure)
/// </summary>
public class InferenceEngine
{
    public SolveResult FindClosure(Concept concept, Dictionary<string, object> initialFacts, List<string> targets)
    {
        var result = new SolveResult
        {
            ConceptName = concept.Name,
            Success = false,
            DerivedFacts = new Dictionary<string, object>()
        };

        var knownFacts = new Dictionary<string, object>(initialFacts);
        bool factAdded = true;
        int stepCount = 1;

        result.Steps.Add($"Step 0: Initializing GT (Known Facts) = {{ {string.Join(", ", knownFacts.Select(k => $"{k.Key}={k.Value}"))} }}");
        result.Steps.Add($"Goal KL (Targets) = {{ {string.Join(", ", targets)} }}");

        // Forward Chaining loop to find Fclosure(GT)
        while (factAdded)
        {
            factAdded = false;

            // Check stopping condition (All KL solved)
            if (targets.All(t => knownFacts.ContainsKey(t)))
            {
                result.Steps.Add("Stopping condition met: All target variables KL are found in GT.");
                result.Success = true;
                break;
            }

            // 1. Iterate over Computation Relations (Rf)
            foreach (var comp in concept.CompRels)
            {
                // Skip if this computation already exists in known facts
                if (knownFacts.ContainsKey(comp.ResultVariable))
                    continue;

                // Check if all input variables for this formula are known
                if (comp.InputVariables.All(v => knownFacts.ContainsKey(v)))
                {
                    try
                    {
                        // Calculate new fact using the formula
                        var value = EvaluateFormula(comp.Expression, knownFacts);
                        knownFacts[comp.ResultVariable] = value;
                        result.DerivedFacts[comp.ResultVariable] = value;

                        result.Steps.Add($"Step {stepCount++}: From Computation {{ {string.Join(", ", comp.InputVariables)} }} => {comp.ResultVariable} = {value}");
                        factAdded = true;
                    }
                    catch (Exception ex)
                    {
                        result.Steps.Add($"Step {stepCount++}: Error evaluating formula '{comp.Expression}': {ex.Message}");
                    }
                }
            }

            // TODO: (Phase 5) Iterate over Rules (Rr) combining logic equations
        }

        if (!result.Success)
        {
            result.ErrorMessage = "Reasoning engine halted: FClosure(GT) exhausted without reaching Goal(KL). Insufficient Facts or Rules.";
        }

        return result;
    }

    private object EvaluateFormula(string formula, Dictionary<string, object> parameters)
    {
        // Replace " with nothing
        var safeFormula = formula.Replace("\"", "");
        
        // Wrap any dot-notation variables (e.g., p1.x) with brackets ([p1.x]) so NCalc parses them correctly
        // We match sequences of alphanumeric and dots that contain at least one dot
        var regex = new System.Text.RegularExpressions.Regex(@"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z0-9_\.]+)\b");
        safeFormula = regex.Replace(safeFormula, "[$1]");

        NCalc.Expression e = new NCalc.Expression(safeFormula);
        
        foreach (var param in parameters)
        {
            e.Parameters[param.Key] = param.Value;
        }

        var result = e.Evaluate();
        
        if (result is int or long or double or float or decimal)
        {
            return Convert.ToDouble(result);
        }
        return result;
    }
}
