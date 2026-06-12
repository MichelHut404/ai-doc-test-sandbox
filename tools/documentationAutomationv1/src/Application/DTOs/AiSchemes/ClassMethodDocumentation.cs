using documentationAutomationv1.Application.Interfaces;
namespace documentationAutomationv1.Application.DTOs;

public record ClassMethodDocumentation(string FileName, string FileDescription, IReadOnlyList<ClassDoc> Classes) : IDocumentationOutput;

public record ClassDoc(string ClassName, string Description, IReadOnlyList<VariableDoc> Variables, IReadOnlyList<MethodDoc> Methods);
public record VariableDoc(string Name, string Type, string Description);
public record MethodDoc(string Signature, string Description, IReadOnlyList<ParameterDoc> Parameters, string Returns);
public record ParameterDoc(string Name, string Type, string Description);