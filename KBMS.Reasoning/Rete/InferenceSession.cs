using System;
using System.Collections.Generic;

namespace KBMS.Reasoning.Rete;

public class InferenceSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    
    public int RulesFiredCount { get; set; } = 0;
    public int InferenceCost { get; set; } = 0;
    
    public List<string> ConflictHistory { get; set; } = new();
    
    // Global tracking of reasoning steps and derived variables across all rules
    public List<ReasoningStep> AuditTrail { get; } = new();
    public HashSet<string> GeneratedVariables { get; } = new(StringComparer.OrdinalIgnoreCase);
    
    // Constants like Pi, Gravity, or context specific settings.
    public Dictionary<string, object> GlobalFacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // --- Rete State ---
    public List<Fact> WorkingMemory { get; } = new();
    public Agenda Agenda { get; } = new();
    public Dictionary<string, object> ExternalFacts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Queue<Fact> PendingFacts { get; } = new();
    public bool IsPropagating { get; set; } = false;
    public Action<string>? Logger { get; set; }

    // --- Node Memories ---
    private readonly Dictionary<Guid, List<Token>> _nodeMemories = new();
    
    public List<Token> GetNodeMemory(Guid nodeId)
    {
        if (!_nodeMemories.TryGetValue(nodeId, out var mem))
        {
            mem = new List<Token>();
            _nodeMemories[nodeId] = mem;
        }
        return mem;
    }

    private readonly Dictionary<Guid, List<Token>> _betaLeftMemories = new();
    public List<Token> GetBetaLeftMemory(Guid nodeId)
    {
        if (!_betaLeftMemories.TryGetValue(nodeId, out var mem))
        {
            mem = new List<Token>();
            _betaLeftMemories[nodeId] = mem;
        }
        return mem;
    }

    private readonly Dictionary<Guid, List<Token>> _betaRightMemories = new();
    public List<Token> GetBetaRightMemory(Guid nodeId)
    {
        if (!_betaRightMemories.TryGetValue(nodeId, out var mem))
        {
            mem = new List<Token>();
            _betaRightMemories[nodeId] = mem;
        }
        return mem;
    }
}
