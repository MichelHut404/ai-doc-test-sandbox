# documentationAutomationv1

A CLI tool that automatically generates Markdown documentation for changed source files in a Git repository. The tool uses Azure AI Foundry (Azure OpenAI) to generate various types of documentation.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Getting the tool into your project](#getting-the-tool-into-your-project)
3. [Setting up an AI model on Azure AI Foundry](#setting-up-an-ai-model-on-azure-ai-foundry)
4. [Configuration](#configuration)
5. [Running via GitHub Actions](#running-via-github-actions)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [GitHub CLI (`gh`)](https://cli.github.com/) — logged in to the correct organisation/repository
- An Azure AI Foundry project with a deployed model (see [section 3](#setting-up-an-ai-model-on-azure-ai-foundry))

---

## Getting the tool into your project

The tool lives in a `tools/` folder inside the repository you want to document. Create this folder if it does not yet exist and copy (or clone) the tool into it:

```
<your-repository>/
└── tools/
    └── documentationAutomationv1
```

Once the tool runs, it automatically performs the following steps:

1. Loads `docsettings.json` from the repository root.
2. Creates a temporary documentation branch: `docs/<current-branch>-<timestamp>`.
3. Retrieves all changed files compared to the previous commit (`HEAD~1..HEAD`).
4. Filters files by extension and the exclusion patterns from `docsettings.json`.
5. Generates Markdown documentation via Azure AI Foundry.
6. Writes the documentation to `docs/generated/` on the documentation branch.
7. Commits and pushes the documentation branch.
8. Creates a pull request via the GitHub CLI.

---

## Quick-start guide: getting the tool working

A concise overview of everything you need to do to get the tool fully working:

1. **Place the tool in your repository** — Copy the `documentationAutomationv1` folder to `tools/documentationAutomationv1/` in the root of the repository you want to document.
2. **Create an Azure Resource Group** — Create a resource group in the [Azure Portal](https://portal.azure.com).
3. **Create an Azure OpenAI resource** — Create an Azure OpenAI resource inside the resource group you just created.
4. **Create an Azure AI Foundry project** — Create a project on [ai.azure.com](https://ai.azure.com) and link it to the Azure OpenAI resource.
5. **Deploy a model** — Deploy a model (`gpt-4o-mini` or `gpt-4o`) via **Deployments** in Azure AI Foundry and note the deployment name. 
6. **Retrieve the endpoint and API key** — Copy the endpoint and API key from **Keys and Endpoint** in the Azure Portal.
7. **Create `docsettings.json`** — Create this file in the root of your repository and set `languagefileextension` and optionally `exclude` (see [Configuration](#configuration)).
8. **Create GitHub Secrets** — Add `OPENAI_APIKEY`, `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENTNAME` via **Settings → Secrets and variables → Actions** in your GitHub repository.
9. **Create the GitHub Actions workflow** — Create `.github/workflows/pipeline.yml` in your repository with the content from [Running via GitHub Actions](#running-via-github-actions).

---

## Setting up an AI model on Azure AI Foundry

The tool uses the Azure OpenAI service via Azure AI Foundry. Follow the steps below to set everything up from scratch: from creating the Azure resources to deploying a model and retrieving the required credentials.

### 1. Create a Resource Group

1. Go to [portal.azure.com](https://portal.azure.com) and sign in.
2. Search for **Resource groups** in the top search bar and click it.
3. Click **+ Create**.
4. Choose your **subscription**, give the resource group a name (e.g. `rg-documentation-tool`) and select a **region** close to your users.
5. Click **Review + create** → **Create**.

### 2. Create an Azure OpenAI resource

1. Go to [portal.azure.com](https://portal.azure.com).
2. Search for **Azure OpenAI** in the top search bar and click **Azure OpenAI** under *Services*.
3. Click **+ Create**.
4. Fill in the following fields:

   | Field | Value |
   |---|---|
   | **Subscription** | Your Azure subscription |
   | **Resource group** | The resource group you created in step 1 |
   | **Region** | Choose a region that supports the desired model (see [model availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability)) |
   | **Name** | A unique name — this becomes part of the endpoint: `https://<name>.openai.azure.com/` |
   | **Pricing tier** | Standard S0 |

5. Click **Review + create** → **Create**.
6. Wait for the deployment to complete and click **Go to resource**.

### 3. Create an Azure AI Foundry project

1. Go to [ai.azure.com](https://ai.azure.com) and sign in.
2. Click **+ New project** and follow the wizard.
3. Link the project to the **subscription** and **resource group** you created in the previous steps.

> Azure AI Foundry automatically creates a hub inside the resource group if one does not yet exist.

### 4. Deploy a model <span style="color:red">**Important**</span>

1. Open your project in Azure AI Foundry.
2. Go to **Deployments** → **+ Deploy model** → **Deploy base model**.
3. Select a suitable model, for example **gpt-4o-mini** or **gpt-4o**.
4. Give the deployment a name (this becomes the `DeploymentName` in the configuration).
5. Choose the desired quota and click **Deploy**.

> The tool uses API version `2024-10-21`. Make sure the chosen model supports this version.

### 5. Retrieve the endpoint and API key <span style="color:red">**Important**</span>

The required values can be found in the Azure portal, on the Azure OpenAI resource you created in step 2:

1. Go to [portal.azure.com](https://portal.azure.com) and open the Azure Foundry resource.
2. Click **Keys and Endpoint** in the left-hand menu.
3. Copy the following values — you will need them in the next step:
4. The deployment name can be found in the Foundry portal. In the top right you see **Operate** — navigate there, then on the left you see **Assets**. Click it, then click **Models** to see the deployment names of your models.

| Value | Where to find it |
|---|---|
| **Endpoint** | *Endpoint* — format: `https://<name>.openai.azure.com/` |
| **API Key** | *Key 1* or *Key 2* |
| **Deployment Name** | The name you provided in step 4 |

---

## Configuration <span style="color:red">**Important**</span>

### docsettings.json

The `docsettings.json` file determines which files the tool documents. Place it in the **root of the repository** you want to document (next to the `.git` folder, not inside the tool folder itself). The tool searches upward from the working directory until the repository root is reached.

#### Structure

```json
{
  "languagefileextension": "cs",
  "exclude": [
    "**/*.Designer.cs",
    "tests/**",
    "**/TestIgnore.cs"
  ]
}
```

#### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `languagefileextension` | `string` | Yes | File extension (without dot) of the source files to document. For example `cs` for C#, `ts` for TypeScript. |
| `exclude` | `string[]` | No | List of [glob patterns](https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing) for files that should **not** be documented. Paths are relative to the repository root. |

#### Example exclude patterns

| Pattern | Effect |
|---|---|
| `**/*.Designer.cs` | Excludes all generated Designer files |
| `tests/**` | Excludes the entire test folder |
| `**/Migrations/**` | Excludes all EF Core migration folders |
| `src/Generated/**` | Excludes a specific generated folder |

> The tool always automatically excludes its own `tools/` folder, regardless of the contents of `docsettings.json`.

### Creating GitHub Secrets <span style="color:red">**Important**</span>

Use the values you retrieved in [section 3, step 5](#5-retrieve-the-endpoint-and-api-key) to create three secrets in GitHub. Go to your repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Name (in GitHub) | Value |
|---|---|
| `OPENAI_APIKEY` | The API key of your Azure AI Foundry deployment |
| `AZURE_OPENAI_ENDPOINT` | The endpoint, e.g. `https://<name>.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENTNAME` | The name of the model deployment |

<span style="color:red">**Important**</span>
> In addition to the secrets, you also need to grant the tool the correct permissions in GitHub. Navigate to your repository → **Settings** → **Actions** → **General**. Scroll all the way down and under **Workflow permissions** enable **Allow GitHub Actions to create and approve pull requests**.

### Linking configuration values in the workflow

The environment variable names follow the .NET convention where a double underscore (`__`) represents a hierarchy separator. `AzureOpenAI__ApiKey` is therefore equivalent to the configuration key `AzureOpenAI:ApiKey` in code.

| Environment variable | Required | Value |
|---|---|---|
| `AzureOpenAI__ApiKey` | Yes | The API key of your Azure AI Foundry deployment |
| `AzureOpenAI__Endpoint` | Yes | The endpoint, e.g. `https://<name>.openai.azure.com/` |
| `AzureOpenAI__DeploymentName` | Yes | The name of the model deployment |
| `Documentation__BasePath` | No | Path where documentation is written (default: `docs`) |

---

## Running via GitHub Actions

The tool can run automatically after every push to a branch. The workflow ensures documentation is generated for all files changed in the relevant commit.

### 1. Create the workflow file <span style="color:red">**Important**</span>

Create the file `.github/workflows/pipeline.yml` in the root of your repository with the following content:

```yaml
name: Generate AI Documentation

on:
  push:
    branches:
      - '**'  # runs on every push, on every branch

jobs:
  build-and-run:
    runs-on: ubuntu-latest
    permissions:
      contents: write       # required to create a new branch and push to it
      pull-requests: write  # required to create a pull request via the gh CLI
    env:
      FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true  # ensures JS-based actions run on Node.js 24

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2  # fetches HEAD and HEAD~1; the tool needs both commits to detect changed files

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Configure Git
        run: |
          # set the identity git uses for the automatic commit
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          # attach the GITHUB_TOKEN to the remote URL so git push works without an SSH key
          git remote set-url origin https://x-access-token:${{ secrets.GITHUB_TOKEN }}@github.com/${{ github.repository }}

      - name: Run AI Doc Tool
        env:
          AzureOpenAI__DeploymentName: ${{ secrets.AZURE_OPENAI_DEPLOYMENTNAME }}  # name of the model deployment in Foundry
          AzureOpenAI__Endpoint:       ${{ secrets.AZURE_OPENAI_ENDPOINT }}         # Azure OpenAI endpoint URL
          AzureOpenAI__ApiKey:         ${{ secrets.OPENAI_APIKEY }}                 # API key for authentication
          Documentation__BasePath:     docs/generated                               # folder where the markdown files are written
          GH_TOKEN:                    ${{ secrets.GITHUB_TOKEN }}                  # used by the gh CLI to create the pull request
        run: |
          # start the tool via the .csproj path so dotnet run finds the correct project
          dotnet run --project tools/documentationAutomationv1/src/Presentation/documentationAutomationv1.Presentation.csproj
```

### 2. Workflow settings explained

| Setting | Reason |
|---|---|
| `fetch-depth: 2` | The tool compares `HEAD~1..HEAD` to find changed files. Without this setting the Git history is too shallow. |
| `permissions: contents: write` | Required to create the documentation branch and push the commit. |
| `permissions: pull-requests: write` | Required so the GitHub CLI (`gh pr create`) can open a pull request. |
| `git remote set-url` | Attaches the `GITHUB_TOKEN` to the remote URL so `git push` works without a separate SSH key. |
| `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24` | Ensures JavaScript-based actions run on Node.js 24 instead of an outdated version. |
| `GH_TOKEN` | The `gh` CLI picks this up automatically. `secrets.GITHUB_TOKEN` is available in every GitHub Actions workflow without extra configuration. |
| `Documentation__BasePath` | Writes the generated documentation to `docs/generated/` in the repository. |
| `--project <path>` | Specifies the exact project so `dotnet run` finds the correct entry point regardless of the working directory. |

### 3. Behaviour when the pipeline encounters an error

If the tool encounters an error (for example a missing configuration value or a failed git command), it exits with a non-zero exit code. By default GitHub Actions marks the current job as failed and skips all subsequent steps in that job.

There are two ways to handle this depending on your situation:

**Option 1 — Let the pipeline fail (default)**

The workflow from [section 1](#1-create-the-workflow-file) is already configured with this behaviour. If the tool fails, the job fails and all subsequent steps are skipped. This ensures a failed documentation step is immediately visible in the pipeline overview.

**Option 2 — Documentation and other jobs independent of each other**

If your pipeline contains other jobs unrelated to documentation (such as a deployment), it is better to place the documentation tool in a **separate job** without other jobs depending on it. This way the deployment continues as normal, while the pipeline overview still clearly shows which job failed.

```yaml
name: CI

on:
  push:
    branches:
      - '**'

jobs:
  generate-docs:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      pull-requests: write
    env:
      FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Configure Git
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git remote set-url origin https://x-access-token:${{ secrets.GITHUB_TOKEN }}@github.com/${{ github.repository }}
      - name: Run AI Doc Tool
        env:
          AzureOpenAI__DeploymentName: ${{ secrets.AZURE_OPENAI_DEPLOYMENTNAME }}
          AzureOpenAI__Endpoint:       ${{ secrets.AZURE_OPENAI_ENDPOINT }}
          AzureOpenAI__ApiKey:         ${{ secrets.OPENAI_APIKEY }}
          Documentation__BasePath:     docs/generated
          GH_TOKEN:                    ${{ secrets.GITHUB_TOKEN }}
        run: dotnet run --project tools/documentationAutomationv1/src/Presentation/documentationAutomationv1.Presentation.csproj

  deploy:
    runs-on: ubuntu-latest
    # no 'needs: generate-docs' → starts in parallel with generate-docs, regardless of its outcome
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Build
        run: dotnet build
      - name: Deploy
        run: echo "Deployment step here"
```

> Only use `continue-on-error: true` on a step when you intentionally want to ignore the error. The downside is that the job as a whole is marked green even if the documentation step failed.

---
