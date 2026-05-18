using documentationAutomationv1.Application.Interfaces;

namespace documentationAutomationv1.Application.DTOs;

public record RelationshipDocumentation( string Summary, IReadOnlyList<RelationshipDoc> Relationships) : IDocumentationOutput
{

}

public record RelationshipDoc(string ClassName, string Inherits, IReadOnlyList<string> Implements, IReadOnlyList<string> Uses);