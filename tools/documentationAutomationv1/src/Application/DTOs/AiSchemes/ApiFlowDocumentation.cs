using documentationAutomationv1.Application.Interfaces;

namespace documentationAutomationv1.Application.DTOs;
public record ApiFlowDocumentation( string Summary, IReadOnlyList<EndpointDoc> Endpoints) : IDocumentationOutput;
public record EndpointDoc(string Method, string Route, string Description, string Input, string Output);