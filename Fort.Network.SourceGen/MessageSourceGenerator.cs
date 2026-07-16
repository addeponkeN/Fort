using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fort.Network.SourceGen;

[Generator]
public class MessageSourceGenerator : ISourceGenerator
{
    private void Log(GeneratorExecutionContext context, string id, string title, string text)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, title, text, "GEN", DiagnosticSeverity.Info, true),
            Location.None));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new MessageSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MessageSyntaxReceiver receiver)
            return;

        Log(context, "GEN001", "NetData Generator running", $"Generating {receiver.CandidateStructs.Count} NetData structs");

        foreach (var structDeclaration in receiver.CandidateStructs)
        {
            var semanticModel = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree);
            var structSymbol = semanticModel.GetDeclaredSymbol(structDeclaration) as INamedTypeSymbol;

            if (structSymbol == null)
                continue;

            // Check if struct has [NetData]
            if (!HasNetDataAttribute(structSymbol))
                continue;

            var fields = CollectFields(context, structDeclaration);

            var source = GenerateMessageImplementation(structSymbol, fields);

            var fileName = $"{structSymbol.Name}.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));

            Log(context, "GEN002", "NetData Generator running", $"Generated {fileName}");
        }
    }

    private static bool HasNetDataAttribute(INamedTypeSymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var fullName = attr.AttributeClass?.ToDisplayString();
            if (fullName == "Fort.Network.NetDataAttribute")
                return true;
        }
        return false;
    }

    private static List<FieldInfo> CollectFields(GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
    {
        var fields = new List<FieldInfo>();

        var semanticModel = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree);

        foreach (var member in structDeclaration.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDeclaration)
                continue;

            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;

                if (fieldSymbol == null)
                    continue;

                fields.Add(new FieldInfo
                {
                    Name = fieldSymbol.Name,
                    TypeName = fieldSymbol.Type.ToDisplayString(),
                    TypeSymbol = fieldSymbol.Type
                });
            }
        }

        return fields;
    }

    private string GenerateMessageImplementation(INamedTypeSymbol structSymbol, List<FieldInfo> fields)
    {
        var namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
        var structName = structSymbol.Name;

        var sb = new StringBuilder();

        sb.AppendLine("using Fort.Network;");
        sb.AppendLine("using LiteNetLib.Utils;");
        sb.AppendLine();

        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        sb.AppendLine($"    public partial struct {structName} : IMessage");
        sb.AppendLine("    {");

        sb.AppendLine("        public void Serialize(NetDataWriter writer)");
        sb.AppendLine("        {");

        foreach (var field in fields)
        {
            sb.AppendLine($"            {GetSerializeCall(field)};");
        }

        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        public void Deserialize(NetDataReader reader)");
        sb.AppendLine("        {");

        foreach (var field in fields)
        {
            sb.AppendLine($"            {GetDeserializeCall(field)};");
        }

        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GetSerializeCall(FieldInfo field)
    {
        return field.TypeName.ToLower() switch
        {
            "byte" => $"writer.Put({field.Name})",
            "sbyte" => $"writer.Put({field.Name})",
            "bool" => $"writer.Put({field.Name})",
            "short" => $"writer.Put({field.Name})",
            "ushort" => $"writer.Put({field.Name})",
            "int" => $"writer.Put({field.Name})",
            "uint" => $"writer.Put({field.Name})",
            "long" => $"writer.Put({field.Name})",
            "ulong" => $"writer.Put({field.Name})",
            "float" => $"writer.Put({field.Name})",
            "double" => $"writer.Put({field.Name})",
            "string" => $"writer.Put({field.Name})",
            "char" => $"writer.Put({field.Name})",
            _ => $"{field.Name}.Serialize(writer)"
        };
    }

    private string GetDeserializeCall(FieldInfo field)
    {
        return field.TypeName.ToLower() switch
        {
            "byte" => $"{field.Name} = reader.GetByte()",
            "sbyte" => $"{field.Name} = reader.GetSByte()",
            "bool" => $"{field.Name} = reader.GetBool()",
            "short" => $"{field.Name} = reader.GetShort()",
            "ushort" => $"{field.Name} = reader.GetUShort()",
            "int" => $"{field.Name} = reader.GetInt()",
            "uint" => $"{field.Name} = reader.GetUInt()",
            "long" => $"{field.Name} = reader.GetLong()",
            "ulong" => $"{field.Name} = reader.GetULong()",
            "float" => $"{field.Name} = reader.GetFloat()",
            "double" => $"{field.Name} = reader.GetDouble()",
            "string" => $"{field.Name} = reader.GetString()",
            "char" => $"{field.Name} = reader.GetChar()",
            _ => $"{field.Name}.Deserialize(reader)"
        };
    }

    private class FieldInfo
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public ITypeSymbol TypeSymbol { get; set; }
    }
}

public class MessageSyntaxReceiver : ISyntaxReceiver
{
    public List<StructDeclarationSyntax> CandidateStructs { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode node)
    {
        if (node is StructDeclarationSyntax structDecl &&
            structDecl.Modifiers.Any(SyntaxKind.PartialKeyword) &&
            structDecl.AttributeLists.Count > 0)
        {
            CandidateStructs.Add(structDecl);
        }
    }
}