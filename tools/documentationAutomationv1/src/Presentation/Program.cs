using documentationAutomationv1.Application.Interfaces;
using documentationAutomationv1.Application.Orchestrators;
using Microsoft.Extensions.Configuration;
using src.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using src.Infrastructure;
using src.Infrastructure.Services;
using src.Application.Services.PromptBuilders;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is niet ingesteld.");
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is niet ingesteld.");
var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is niet ingesteld.");


var services = new ServiceCollection();

services.AddSingleton<IPromptBuilder, ClassMethodPromptBuilder>();
services.AddSingleton<IPromptBuilder, ApiFlowPromptBuilder>();
services.AddSingleton<IPromptBuilder, RelationshipPromptBuilder>();
services.AddSingleton<IChatClient>(_ => new AzureChatClient(apiKey, endpoint, deploymentName));
services.AddSingleton<IAiDocumentationService>(sp =>
    new AiDocumentationService(
        sp.GetRequiredService<IChatClient>(),
        sp.GetServices<IPromptBuilder>()));
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<IProcessRunner, CmdProcessRunner>();
services.AddSingleton<IGitService, GitService>();
var docsBasePath = configuration["Documentation:BasePath"] ?? "docs";
services.AddSingleton<IMarkdownWriterService>(_ => new MarkdownWriterService(docsBasePath));
services.AddSingleton<AzureOrchestrator>();

var serviceProvider = services.BuildServiceProvider();

var orchestrator = serviceProvider.GetRequiredService<AzureOrchestrator>();
await orchestrator.RunAsync();

