using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fort.Network.SourceGen;

[Generator]
public class MessageSourceGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
		context.RegisterForSyntaxNotifications(() => new MessageSyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (!(context.SyntaxReceiver is MessageSyntaxReceiver receiver))
			return;

		// groupby - avoid dupes
		var structGroups = receiver.CandidateStructs
			.GroupBy(s => s.Identifier.ValueText)
			.ToList();

		foreach (var structGroup in structGroups)
		{
			var firstStruct = structGroup.First();
			var semanticModel = context.Compilation.GetSemanticModel(firstStruct.SyntaxTree);
			var structSymbol = semanticModel.GetDeclaredSymbol(firstStruct) as INamedTypeSymbol;

			if (structSymbol == null)
				continue;

			// maybe too strict? struct msg must end with 'Message'
			if (!structSymbol.Name.EndsWith("Message"))
				continue;

			// collect all fields of the msg struct
			var allFields = new List<FieldInfo>();
			foreach (var structDeclaration in structGroup)
			{
				var currentSemanticModel = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree);
				foreach (var member in structDeclaration.Members)
				{
					if (member is FieldDeclarationSyntax fieldDeclaration)
					{
						foreach (var variable in fieldDeclaration.Declaration.Variables)
						{
							var fieldName = variable.Identifier.ValueText;
							if (allFields.All(f => f.Name != fieldName))
							{
								var fieldSymbol = currentSemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
								var typeName = fieldDeclaration.Declaration.Type.ToString();
								allFields.Add(new FieldInfo
								{
									Name = fieldName,
									TypeName = typeName,
									TypeSymbol = fieldSymbol?.Type
								});
							}
						}
					}
				}
			}

			// generate the message .cs file
			var source = GenerateMessageImplementation(structSymbol, allFields);
			string fileName = $"{structSymbol.Name}.g.cs";
			context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
			Console.WriteLine($"Generated '{fileName}'");
		}
	}

	private string GenerateMessageImplementation(INamedTypeSymbol structSymbol, List<FieldInfo> fields)
	{
		var namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
		var structName = structSymbol.Name;

		var sb = new StringBuilder();

		// add using
		sb.AppendLine("using Fort.Network;");
		sb.AppendLine("using LiteNetLib.Utils;");
		sb.AppendLine();

		// add namespace
		sb.AppendLine($"namespace {namespaceName}");
		sb.AppendLine("{");

		// add class & inheritance
		sb.AppendLine($"    public partial struct {structName} : IMessage");
		sb.AppendLine("    {");

		// serialize method
		sb.AppendLine("        public void Serialize(NetDataWriter writer)");
		sb.AppendLine("        {");
		foreach (var field in fields)
		{
			sb.AppendLine($"            {GetSerializeCall(field)};");
		}
		sb.AppendLine("        }");
		sb.AppendLine();

		// deserialize method
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

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		if (syntaxNode is StructDeclarationSyntax structDeclaration &&
			structDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
		{
			CandidateStructs.Add(structDeclaration);
		}
	}
}