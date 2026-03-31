using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KBMS.Models;
using Tuple = KBMS.Storage.V3.Tuple;

namespace KBMS.Storage.V3;

public static class ModelBinaryUtility
{
    // ============================================
    // KNOWLEDGE BASE SERIALIZATION
    // ============================================

    public static byte[] SerializeKb(KnowledgeBase kb)
    {
        var tuple = new Tuple();
        tuple.AddGuid(kb.Id);
        tuple.AddGuid(kb.OwnerId);
        tuple.AddString(kb.Name);
        tuple.AddLong(kb.CreatedAt.Ticks);
        tuple.AddString(kb.Description);
        tuple.AddInt(kb.ObjectCount);
        tuple.AddInt(kb.RuleCount);
        tuple.Fields.Add(SerializeList(kb.Rules, WriteRule));
        tuple.Fields.Add(SerializeList(kb.Relations, WriteRelation));
        tuple.Fields.Add(SerializeList(kb.Operators, WriteOperator));
        tuple.Fields.Add(SerializeList(kb.Functions, WriteFunction));
        tuple.Fields.Add(SerializeList(kb.Hierarchies, WriteHierarchy));
        return tuple.Serialize();
    }

    public static KnowledgeBase? DeserializeKb(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try
        {
            var tuple = Tuple.Deserialize(data);
            return new KnowledgeBase
            {
                Id = tuple.GetGuid(0),
                OwnerId = tuple.GetGuid(1),
                Name = tuple.GetString(2),
                CreatedAt = new DateTime(tuple.GetLong(3)),
                Description = tuple.GetString(4),
                ObjectCount = tuple.GetInt(5),
                RuleCount = tuple.GetInt(6),
                Rules = DeserializeList(tuple.Fields[7], ReadRule),
                Relations = DeserializeList(tuple.Fields[8], ReadRelation),
                Operators = DeserializeList(tuple.Fields[9], ReadOperator),
                Functions = DeserializeList(tuple.Fields[10], ReadFunction),
                Hierarchies = DeserializeList(tuple.Fields[11], ReadHierarchy)
            };
        }
        catch { return null; }
    }

    // ============================================
    // CONCEPT SERIALIZATION
    // ============================================

    public static byte[] SerializeConcept(Concept concept)
    {
        var tuple = new Tuple();
        tuple.AddGuid(concept.Id);
        tuple.AddGuid(concept.KbId);
        tuple.AddString(concept.Name);
        
        tuple.Fields.Add(SerializeList(concept.Variables, WriteVariable));
        tuple.Fields.Add(SerializeList(concept.Constraints, WriteConstraint));
        tuple.Fields.Add(SerializeList(concept.CompRels, WriteCompRel));
        tuple.Fields.Add(SerializeList(concept.Aliases, WriteString));
        tuple.Fields.Add(SerializeList(concept.BaseObjects, WriteString));
        tuple.Fields.Add(SerializeList(concept.SameVariables, WriteSameVariable));
        tuple.Fields.Add(SerializeList(concept.ConstructRelations, WriteConstructRel));
        tuple.Fields.Add(SerializeList(concept.Properties, WriteProperty));
        tuple.Fields.Add(SerializeList(concept.ConceptRules, WriteConceptRule));
        tuple.Fields.Add(SerializeList(concept.Equations, WriteEquation));

        return tuple.Serialize();
    }

    public static Concept? DeserializeConcept(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try
        {
            var tuple = Tuple.Deserialize(data);
            return new Concept
            {
                Id = tuple.GetGuid(0),
                KbId = tuple.GetGuid(1),
                Name = tuple.GetString(2),
                Variables = DeserializeList(tuple.Fields[3], ReadVariable),
                Constraints = DeserializeList(tuple.Fields[4], ReadConstraint),
                CompRels = DeserializeList(tuple.Fields[5], ReadCompRel),
                Aliases = DeserializeList(tuple.Fields[6], ReadString),
                BaseObjects = DeserializeList(tuple.Fields[7], ReadString),
                SameVariables = DeserializeList(tuple.Fields[8], ReadSameVariable),
                ConstructRelations = DeserializeList(tuple.Fields[9], ReadConstructRel),
                Properties = DeserializeList(tuple.Fields[10], ReadProperty),
                ConceptRules = DeserializeList(tuple.Fields[11], ReadConceptRule),
                Equations = DeserializeList(tuple.Fields[12], ReadEquation)
            };
        }
        catch { return null; }
    }

    // ============================================
    // OTHER METADATA MODELS (H, R, Ops, Funcs, Rules)
    // ============================================

    public static byte[] SerializeHierarchy(Hierarchy h)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, true);
        bw.Write(h.Id.ToByteArray());
        bw.Write(h.KbId.ToByteArray());
        bw.Write(h.ParentConcept ?? string.Empty);
        bw.Write(h.ChildConcept ?? string.Empty);
        bw.Write((int)h.HierarchyType);
        return ms.ToArray();
    }

    public static Hierarchy? DeserializeHierarchy(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.UTF8, true);
            return new Hierarchy {
                Id = new Guid(br.ReadBytes(16)),
                KbId = new Guid(br.ReadBytes(16)),
                ParentConcept = br.ReadString(),
                ChildConcept = br.ReadString(),
                HierarchyType = (HierarchyType)br.ReadInt32()
            };
        } catch { return null; }
    }

    public static byte[] SerializeRule(Rule r)
    {
        var tuple = new Tuple();
        tuple.AddGuid(r.Id);
        tuple.AddGuid(r.KbId);
        tuple.AddString(r.Name);
        tuple.AddString(r.RuleType);
        tuple.AddString(r.Scope);
        tuple.AddInt(r.Cost);
        tuple.Fields.Add(SerializeList(r.Hypothesis, WriteExpression));
        tuple.Fields.Add(SerializeList(r.Conclusion, WriteExpression));
        return tuple.Serialize();
    }

    public static Rule? DeserializeRule(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try {
            var tuple = Tuple.Deserialize(data);
            return new Rule {
                Id = tuple.GetGuid(0),
                KbId = tuple.GetGuid(1),
                Name = tuple.GetString(2),
                RuleType = tuple.GetString(3),
                Scope = tuple.GetString(4),
                Cost = tuple.GetInt(5),
                Hypothesis = DeserializeList(tuple.Fields[6], ReadExpression),
                Conclusion = DeserializeList(tuple.Fields[7], ReadExpression)
            };
        } catch { return null; }
    }

    public static byte[] SerializeRelation(Relation r)
    {
        var tuple = new Tuple();
        tuple.AddGuid(r.Id);
        tuple.AddGuid(r.KbId);
        tuple.AddString(r.Name);
        tuple.AddString(r.Domain);
        tuple.AddString(r.Range);
        tuple.Fields.Add(SerializeList(r.Properties, WriteString));
        tuple.Fields.Add(SerializeList(r.ParamNames, WriteString));
        tuple.Fields.Add(SerializeList(r.Equations, WriteEquation));
        tuple.Fields.Add(SerializeList(r.Rules, WriteConceptRule));
        return tuple.Serialize();
    }

    public static Relation? DeserializeRelation(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try {
            var tuple = Tuple.Deserialize(data);
            return new Relation {
                Id = tuple.GetGuid(0),
                KbId = tuple.GetGuid(1),
                Name = tuple.GetString(2),
                Domain = tuple.GetString(3),
                Range = tuple.GetString(4),
                Properties = DeserializeList(tuple.Fields[5], ReadString),
                ParamNames = DeserializeList(tuple.Fields[6], ReadString),
                Equations = DeserializeList(tuple.Fields[7], ReadEquation),
                Rules = DeserializeList(tuple.Fields[8], ReadConceptRule)
            };
        } catch { return null; }
    }

    public static byte[] SerializeOperator(Operator o)
    {
        var tuple = new Tuple();
        tuple.AddGuid(o.Id);
        tuple.AddGuid(o.KbId);
        tuple.AddString(o.Symbol);
        tuple.AddString(o.ReturnType);
        tuple.AddString(o.Body);
        tuple.Fields.Add(SerializeList(o.ParamTypes, WriteString));
        tuple.Fields.Add(SerializeList(o.Properties, WriteString));
        return tuple.Serialize();
    }

    public static Operator? DeserializeOperator(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try {
            var tuple = Tuple.Deserialize(data);
            return new Operator {
                Id = tuple.GetGuid(0),
                KbId = tuple.GetGuid(1),
                Symbol = tuple.GetString(2),
                ReturnType = tuple.GetString(3),
                Body = tuple.GetString(4),
                ParamTypes = DeserializeList(tuple.Fields[5], ReadString),
                Properties = DeserializeList(tuple.Fields[6], ReadString)
            };
        } catch { return null; }
    }

    public static byte[] SerializeFunction(Function f)
    {
        var tuple = new Tuple();
        tuple.AddGuid(f.Id);
        tuple.AddGuid(f.KbId);
        tuple.AddString(f.Name);
        tuple.AddString(f.ReturnType);
        tuple.AddString(f.Body);
        tuple.Fields.Add(SerializeList(f.Parameters, WriteFunctionParameter));
        tuple.Fields.Add(SerializeList(f.Properties, WriteString));
        return tuple.Serialize();
    }

    public static Function? DeserializeFunction(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        try {
            var tuple = Tuple.Deserialize(data);
            return new Function {
                Id = tuple.GetGuid(0),
                KbId = tuple.GetGuid(1),
                Name = tuple.GetString(2),
                ReturnType = tuple.GetString(3),
                Body = tuple.GetString(4),
                Parameters = DeserializeList(tuple.Fields[5], ReadFunctionParameter),
                Properties = DeserializeList(tuple.Fields[6], ReadString)
            };
        } catch { return null; }
    }

    private static void WriteHierarchy(BinaryWriter bw, Hierarchy h)
    {
        bw.Write(h.Id.ToByteArray());
        bw.Write(h.KbId.ToByteArray());
        bw.Write(h.ParentConcept ?? string.Empty);
        bw.Write(h.ChildConcept ?? string.Empty);
        bw.Write((int)h.HierarchyType);
    }
    
    private static Hierarchy ReadHierarchy(BinaryReader br) => new Hierarchy {
        Id = new Guid(br.ReadBytes(16)),
        KbId = new Guid(br.ReadBytes(16)),
        ParentConcept = br.ReadString(),
        ChildConcept = br.ReadString(),
        HierarchyType = (HierarchyType)br.ReadInt32()
    };

    private static void WriteRule(BinaryWriter bw, Rule r)
    {
        bw.Write(r.Id.ToByteArray());
        bw.Write(r.KbId.ToByteArray());
        bw.Write(r.Name ?? string.Empty);
        bw.Write(r.RuleType ?? string.Empty);
        bw.Write(r.Scope ?? string.Empty);
        bw.Write(r.Cost);
        
        bw.Write(r.Hypothesis.Count);
        foreach(var h in r.Hypothesis) WriteExpression(bw, h);
        
        bw.Write(r.Conclusion.Count);
        foreach(var c in r.Conclusion) WriteExpression(bw, c);
    }
    
    private static Rule ReadRule(BinaryReader br)
    {
        var r = new Rule {
            Id = new Guid(br.ReadBytes(16)),
            KbId = new Guid(br.ReadBytes(16)),
            Name = br.ReadString(),
            RuleType = br.ReadString(),
            Scope = br.ReadString(),
            Cost = br.ReadInt32(),
            Hypothesis = new List<Expression>(),
            Conclusion = new List<Expression>()
        };
        int hCount = br.ReadInt32(); for(int i=0; i<hCount; i++) r.Hypothesis.Add(ReadExpression(br));
        int cCount = br.ReadInt32(); for(int i=0; i<cCount; i++) r.Conclusion.Add(ReadExpression(br));
        return r;
    }

    private static void WriteRelation(BinaryWriter bw, Relation r)
    {
        bw.Write(r.Id.ToByteArray());
        bw.Write(r.KbId.ToByteArray());
        bw.Write(r.Name ?? string.Empty);
        bw.Write(r.Domain ?? string.Empty);
        bw.Write(r.Range ?? string.Empty);
        
        bw.Write(r.Properties.Count); foreach(var p in r.Properties) bw.Write(p ?? string.Empty);
        bw.Write(r.ParamNames.Count); foreach(var p in r.ParamNames) bw.Write(p ?? string.Empty);
        
        bw.Write(r.Equations.Count); foreach(var e in r.Equations) WriteEquation(bw, e);
        bw.Write(r.Rules.Count); foreach(var rule in r.Rules) WriteConceptRule(bw, rule);
    }

    private static Relation ReadRelation(BinaryReader br)
    {
        var r = new Relation {
            Id = new Guid(br.ReadBytes(16)),
            KbId = new Guid(br.ReadBytes(16)),
            Name = br.ReadString(),
            Domain = br.ReadString(),
            Range = br.ReadString(),
            Properties = new List<string>(),
            ParamNames = new List<string>(),
            Equations = new List<Equation>(),
            Rules = new List<ConceptRule>()
        };
        int p1Count = br.ReadInt32(); for(int i=0; i<p1Count; i++) r.Properties.Add(br.ReadString());
        int p2Count = br.ReadInt32(); for(int i=0; i<p2Count; i++) r.ParamNames.Add(br.ReadString());
        int eCount = br.ReadInt32(); for(int i=0; i<eCount; i++) r.Equations.Add(ReadEquation(br));
        int rCount = br.ReadInt32(); for(int i=0; i<rCount; i++) r.Rules.Add(ReadConceptRule(br));
        return r;
    }

    private static void WriteOperator(BinaryWriter bw, Operator o)
    {
        bw.Write(o.Id.ToByteArray());
        bw.Write(o.KbId.ToByteArray());
        bw.Write(o.Symbol ?? string.Empty);
        bw.Write(o.ReturnType ?? string.Empty);
        bw.Write(o.Body ?? string.Empty);
        
        bw.Write(o.ParamTypes.Count); foreach(var p in o.ParamTypes) bw.Write(p ?? string.Empty);
        bw.Write(o.Properties.Count); foreach(var p in o.Properties) bw.Write(p ?? string.Empty);
    }

    private static Operator ReadOperator(BinaryReader br)
    {
        var o = new Operator {
            Id = new Guid(br.ReadBytes(16)),
            KbId = new Guid(br.ReadBytes(16)),
            Symbol = br.ReadString(),
            ReturnType = br.ReadString(),
            Body = br.ReadString(),
            ParamTypes = new List<string>(),
            Properties = new List<string>()
        };
        int p1Count = br.ReadInt32(); for(int i=0; i<p1Count; i++) o.ParamTypes.Add(br.ReadString());
        int p2Count = br.ReadInt32(); for(int i=0; i<p2Count; i++) o.Properties.Add(br.ReadString());
        return o;
    }

    private static void WriteFunction(BinaryWriter bw, Function f)
    {
        bw.Write(f.Id.ToByteArray());
        bw.Write(f.KbId.ToByteArray());
        bw.Write(f.Name ?? string.Empty);
        bw.Write(f.ReturnType ?? string.Empty);
        bw.Write(f.Body ?? string.Empty);
        
        bw.Write(f.Parameters.Count); foreach(var p in f.Parameters) WriteFunctionParameter(bw, p);
        bw.Write(f.Properties.Count); foreach(var p in f.Properties) bw.Write(p ?? string.Empty);
    }

    private static Function ReadFunction(BinaryReader br)
    {
        var f = new Function {
            Id = new Guid(br.ReadBytes(16)),
            KbId = new Guid(br.ReadBytes(16)),
            Name = br.ReadString(),
            ReturnType = br.ReadString(),
            Body = br.ReadString(),
            Parameters = new List<FunctionParameter>(),
            Properties = new List<string>()
        };
        int p1Count = br.ReadInt32(); for(int i=0; i<p1Count; i++) f.Parameters.Add(ReadFunctionParameter(br));
        int p2Count = br.ReadInt32(); for(int i=0; i<p2Count; i++) f.Properties.Add(br.ReadString());
        return f;
    }

    // ============================================
    // GENERIC LIST SERIALIZATION HANDLERS
    // ============================================

    private static byte[] SerializeList<T>(List<T> list, Action<BinaryWriter, T> writeAction)
    {
        if (list == null || list.Count == 0) return Array.Empty<byte>();

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, true);
        
        bw.Write(list.Count);
        foreach (var item in list)
        {
            writeAction(bw, item);
        }
        
        return ms.ToArray();
    }

    private static List<T> DeserializeList<T>(byte[] data, Func<BinaryReader, T> readFunc)
    {
        var list = new List<T>();
        if (data == null || data.Length == 0) return list;

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms, Encoding.UTF8, true);
        
        try 
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                list.Add(readFunc(br));
            }
        }
        catch { /* Return whatever we managed to parse if corruption occurs */ }

        return list;
    }

    // ============================================
    // INDIVIDUAL ELEMENT ENCODERS
    // ============================================

    private static void WriteNullableString(BinaryWriter bw, string? s)
    {
        if (s == null) bw.Write(false);
        else { bw.Write(true); bw.Write(s); }
    }

    private static string? ReadNullableString(BinaryReader br)
    {
        return br.ReadBoolean() ? br.ReadString() : null;
    }

    private static void WriteString(BinaryWriter bw, string s) => bw.Write(s ?? string.Empty);
    private static string ReadString(BinaryReader br) => br.ReadString();

    // -- Variable --
    private static void WriteVariable(BinaryWriter bw, Variable v)
    {
        bw.Write(v.Name ?? string.Empty);
        bw.Write(v.Type ?? string.Empty);
        bw.Write(v.Length.HasValue); if (v.Length.HasValue) bw.Write(v.Length.Value);
        bw.Write(v.Scale.HasValue);  if (v.Scale.HasValue) bw.Write(v.Scale.Value);
    }
    private static Variable ReadVariable(BinaryReader br) => new Variable {
        Name = br.ReadString(),
        Type = br.ReadString(),
        Length = br.ReadBoolean() ? br.ReadInt32() : null,
        Scale = br.ReadBoolean() ? br.ReadInt32() : null
    };

    // -- Constraint --
    private static void WriteConstraint(BinaryWriter bw, Constraint c)
    {
        bw.Write(c.Name ?? string.Empty);
        bw.Write(c.Expression ?? string.Empty);
        bw.Write(c.Line);
        bw.Write(c.Column);
    }
    private static Constraint ReadConstraint(BinaryReader br) => new Constraint {
        Name = br.ReadString(),
        Expression = br.ReadString(),
        Line = br.ReadInt32(),
        Column = br.ReadInt32()
    };

    // -- ComputationRelation --
    private static void WriteCompRel(BinaryWriter bw, ComputationRelation c)
    {
        bw.Write(c.Id.ToByteArray());
        bw.Write(c.ConceptName ?? string.Empty);
        bw.Write(c.Flag);
        
        bw.Write(c.InputVariables.Count);
        foreach(var iv in c.InputVariables) bw.Write(iv ?? string.Empty);
        
        bw.Write(c.Rank);
        WriteNullableString(bw, c.ResultVariable);
        bw.Write(c.Expression ?? string.Empty);
        bw.Write(c.Cost);
    }
    private static ComputationRelation ReadCompRel(BinaryReader br)
    {
        var cr = new ComputationRelation {
            Id = new Guid(br.ReadBytes(16)),
            ConceptName = br.ReadString(),
            Flag = br.ReadInt32(),
            InputVariables = new List<string>()
        };
        int count = br.ReadInt32();
        for(int i=0; i<count; i++) cr.InputVariables.Add(br.ReadString());
        
        cr.Rank = br.ReadInt32();
        cr.ResultVariable = ReadNullableString(br);
        cr.Expression = br.ReadString();
        cr.Cost = br.ReadInt32();
        return cr;
    }

    // -- SameVariable --
    private static void WriteSameVariable(BinaryWriter bw, SameVariable sv)
    {
        bw.Write(sv.Variable1 ?? string.Empty);
        bw.Write(sv.Variable2 ?? string.Empty);
    }
    private static SameVariable ReadSameVariable(BinaryReader br) => new SameVariable {
        Variable1 = br.ReadString(),
        Variable2 = br.ReadString()
    };

    // -- ConstructRelation --
    private static void WriteConstructRel(BinaryWriter bw, ConstructRelation cr)
    {
        bw.Write(cr.RelationName ?? string.Empty);
        bw.Write(cr.Arguments.Count);
        foreach(var a in cr.Arguments) bw.Write(a ?? string.Empty);
    }
    private static ConstructRelation ReadConstructRel(BinaryReader br)
    {
        var cr = new ConstructRelation { RelationName = br.ReadString(), Arguments = new List<string>() };
        int count = br.ReadInt32();
        for(int i=0; i<count; i++) cr.Arguments.Add(br.ReadString());
        return cr;
    }

    // -- Property --
    private static void WriteProperty(BinaryWriter bw, Property p)
    {
        bw.Write(p.Key ?? string.Empty);
        
        string valString = p.Value?.ToString() ?? string.Empty;
        bw.Write(valString);
    }
    private static Property ReadProperty(BinaryReader br) => new Property {
        Key = br.ReadString(),
        Value = br.ReadString() // Approximated back to string for simplicity
    };

    // -- ConceptRule --
    private static void WriteConceptRule(BinaryWriter bw, ConceptRule r)
    {
        bw.Write(r.Id.ToByteArray());
        bw.Write(r.Kind ?? string.Empty);
        
        bw.Write(r.Variables.Count);
        foreach(var v in r.Variables) WriteVariable(bw, v);
        
        bw.Write(r.Hypothesis.Count);
        foreach(var h in r.Hypothesis) bw.Write(h ?? string.Empty);
        
        bw.Write(r.Conclusion.Count);
        foreach(var c in r.Conclusion) bw.Write(c ?? string.Empty);
    }
    private static ConceptRule ReadConceptRule(BinaryReader br)
    {
        var r = new ConceptRule {
            Id = new Guid(br.ReadBytes(16)),
            Kind = br.ReadString(),
            Variables = new List<Variable>(),
            Hypothesis = new List<string>(),
            Conclusion = new List<string>()
        };
        int vCount = br.ReadInt32(); for(int i=0; i<vCount; i++) r.Variables.Add(ReadVariable(br));
        int hCount = br.ReadInt32(); for(int i=0; i<hCount; i++) r.Hypothesis.Add(br.ReadString());
        int cCount = br.ReadInt32(); for(int i=0; i<cCount; i++) r.Conclusion.Add(br.ReadString());
        return r;
    }

    // -- Expression (Recursive) --
    private static void WriteExpression(BinaryWriter bw, Expression e)
    {
        bw.Write(e.Type ?? string.Empty);
        bw.Write(e.Content ?? string.Empty);
        bw.Write(e.Children.Count);
        foreach(var child in e.Children) WriteExpression(bw, child);
    }
    private static Expression ReadExpression(BinaryReader br)
    {
        var e = new Expression {
            Type = br.ReadString(),
            Content = br.ReadString(),
            Children = new List<Expression>()
        };
        int count = br.ReadInt32();
        for(int i=0; i<count; i++) e.Children.Add(ReadExpression(br));
        return e;
    }

    // -- FunctionParameter --
    private static void WriteFunctionParameter(BinaryWriter bw, FunctionParameter p)
    {
        bw.Write(p.Type ?? string.Empty);
        bw.Write(p.Name ?? string.Empty);
    }
    private static FunctionParameter ReadFunctionParameter(BinaryReader br) => new FunctionParameter {
        Type = br.ReadString(),
        Name = br.ReadString()
    };

    // -- Equation --
    private static void WriteEquation(BinaryWriter bw, Equation e)
    {
        bw.Write(e.Id.ToByteArray());
        bw.Write(e.Expression ?? string.Empty);
        bw.Write(e.Line);
        bw.Write(e.Column);
        bw.Write(e.Variables.Count);
        foreach(var v in e.Variables) bw.Write(v ?? string.Empty);
    }
    private static Equation ReadEquation(BinaryReader br)
    {
        var e = new Equation {
            Id = new Guid(br.ReadBytes(16)),
            Expression = br.ReadString(),
            Line = br.ReadInt32(),
            Column = br.ReadInt32(),
            Variables = new List<string>()
        };
        int count = br.ReadInt32();
        for(int i=0; i<count; i++) e.Variables.Add(br.ReadString());
        return e;
    }
}
