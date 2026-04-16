using documentationAutomationv1.Application.Interfaces;
using documentationAutomationv1.Application.Orchestrators;
using Microsoft.Extensions.Configuration;
using src.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using src.Infrastructure;
using src.Infrastructure.Services;
using src.Infrastructure.Services.PromptBuilders;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is niet ingesteld.");
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is niet ingesteld.");
var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is niet ingesteld.");


var services = new ServiceCollection();

services.AddScoped<IPromptBuilder, ClassMethodPromptBuilder>();
services.AddScoped<IPromptBuilder, ApiFlowPromptBuilder>();
services.AddScoped<IPromptBuilder, RelationshipPromptBuilder>();
services.AddScoped<IChatClient>(_ => new AzureChatClient(apiKey, endpoint, deploymentName));
services.AddScoped<IAiDocumentationService>(sp =>
    new AiDocumentationService(
        sp.GetRequiredService<IChatClient>(),
        sp.GetServices<IPromptBuilder>()));
services.AddScoped<ICodeAnalysisService, CodeAnalysisService>();
services.AddScoped<IProcessRunner, CMDProcessRunner>();
services.AddScoped<IGitService, GitService>();
var docsBasePath = configuration["Documentation:BasePath"] ?? "docs";
services.AddScoped<IMarkdownWriterService>(_ => new MarkdownWriterService(docsBasePath));
services.AddScoped<CliOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

var orchestrator = serviceProvider.GetRequiredService<CliOrchestrator>();
await orchestrator.RunAsync();

