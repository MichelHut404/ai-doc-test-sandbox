# documentationAutomationv1

Een CLI-tool die automatisch Markdown-documentatie genereert voor gewijzigde bronbestanden in een Git-repository. De tool gebruikt Azure AI Foundry (Azure OpenAI) om verschillende soorten documentaties te genereren.



---

## Inhoudsopgave

1. [Vereisten](#vereisten)
2. [De tool werkend krijgen in het project](#de-tool-werkend-krijgen-in-het-project)
3. [docsettings.json](#docsettingsjson)
4. [De agent opzetten op Azure AI Foundry](#de-agent-opzetten-op-azure-ai-foundry)
5. [Uitvoeren via GitHub Actions](#uitvoeren-via-github-actions)

---

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [GitHub CLI (`gh`)](https://cli.github.com/) — ingelogd op de juiste organisatie/repository
- Een Azure AI Foundry-project met een gedeployde model (zie [sectie 4](#de-agent-opzetten-op-azure-ai-foundry))

---

## De tool werkend krijgen in het project

### 1. Repository klonen en bouwen

De tool leeft in een `tools/`-map in de repository die je wilt documenteren. Maak deze map aan als die nog niet bestaat en kopieer (of clone) de tool daarin:

```
<jouw-repository>/
└── tools/
    └── documentationAutomationv1
```

Kloon de tool-repository in die map en bouw hem:

```bash
git clone <repository-url>
cd tools/documentationAutomationv1
dotnet build
```

### 2. Configuratiewaarden instellen

De tool verwacht drie verplichte configuratiewaarden. Stel deze in via **User Secrets** (aanbevolen voor lokaal gebruik) of via **omgevingsvariabelen** (aanbevolen voor CI/CD).

#### Via User Secrets (lokaal)

Voer de volgende commando's uit vanuit de `src/Presentation`-map:

```bash
cd src/Presentation
dotnet user-secrets set "AzureOpenAI:ApiKey"        "<jouw-api-key>"
dotnet user-secrets set "AzureOpenAI:Endpoint"      "https://<jouw-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "<deployment-naam>"
```

Optioneel — standaard schrijft de tool documentatie naar de map `docs/` in de repository root. Wil je dit aanpassen:

```bash
dotnet user-secrets set "Documentation:BasePath" "mijn-docs-map"
```

#### Via omgevingsvariabelen (CI/CD)

Bij gebruik in een GitHub Actions-pipeline worden de configuratiewaarden **niet** als losse omgevingsvariabelen in de runner ingesteld, maar als **GitHub Secrets** die via de `env:`-sectie van de workflow aan de tool worden meegegeven. Zie [sectie 5](#uitvoeren-via-github-actions) voor het volledige workflow-bestand.

De namen van de omgevingsvariabelen volgen de .NET-conventie waarbij een dubbele underscore (`__`) een niveaugrens aangeeft in de configuratiehiërarchie. `AzureOpenAI__ApiKey` staat dus gelijk aan de configuratiesleutel `AzureOpenAI:ApiKey` in code.

| Omgevingsvariabele | Verplicht | Waarde |
|---|---|---|
| `AzureOpenAI__ApiKey` | Ja | De API-sleutel van je Azure AI Foundry-deployment |
| `AzureOpenAI__Endpoint` | Ja | Het endpoint, bijv. `https://<naam>.openai.azure.com/` |
| `AzureOpenAI__DeploymentName` | Ja | De naam van de model-deployment |
| `Documentation__BasePath` | Nee | Pad waar documentatie naartoe geschreven wordt (standaard: `docs`) |

**Stap 1 — Secrets aanmaken in GitHub**

Ga naar je repository op GitHub en open **Settings → Secrets and variables → Actions → New repository secret**. Maak de volgende drie secrets aan:

| Naam (in GitHub) | Waarde |
|---|---|
| `OPENAI_APIKEY` | De API-sleutel van je Azure AI Foundry-deployment |
| `AZURE_OPENAI_ENDPOINT` | Het endpoint, bijv. `https://<naam>.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENTNAME` | De naam van de model-deployment |

**Stap 2 — Secrets koppelen in de workflow**

In het workflow-bestand (`.github/workflows/pipeline.yml`, zie sectie 5) koppel je de GitHub Secrets via de `env:`-sectie aan de omgevingsvariabelen die de tool verwacht:

```yaml
env:
  AzureOpenAI__ApiKey:         ${{ secrets.OPENAI_APIKEY }}
  AzureOpenAI__Endpoint:       ${{ secrets.AZURE_OPENAI_ENDPOINT }}
  AzureOpenAI__DeploymentName: ${{ secrets.AZURE_OPENAI_DEPLOYMENTNAME }}
```

GitHub injecteert de waarden van de secrets op het moment dat de workflow draait. De tool leest ze vervolgens als gewone omgevingsvariabelen via de .NET-configuratieprovider. Het volledige workflow-bestand staat in [sectie 5](#uitvoeren-via-github-actions).

### 3. De tool uitvoeren

Zorg dat `docsettings.json` aanwezig is in de repository root (zie [docsettings.json](#docsettingsjson)) voordat je de tool uitvoert.

```bash
cd tools/documentationAutomationv1/src/Presentation
dotnet run
```

De tool voert de volgende stappen automatisch uit:

1. Laadt `docsettings.json` vanuit de repository root.
2. Maakt een tijdelijke documentatiebranch aan: `docs/<huidige-branch>-<tijdstempel>`.
3. Haalt alle gewijzigde bestanden op t.o.v. de vorige commit (`HEAD~1..HEAD`).
4. Filtert bestanden op extensie en de uitsluitingspatronen uit `docsettings.json`.
5. Genereert Markdown-documentatie via Azure AI Foundry.
6. Schrijft de documentatie naar `docs/generated/` op de documentatiebranch.
7. Commit en pusht de documentatiebranch.
8. Maakt een pull request aan via de GitHub CLI.

---

## docsettings.json

Het bestand `docsettings.json` bepaalt welke bestanden de tool documenteert. Plaats het in de **root van de repository** die je wilt documenteren (naast de `.git`-map, dus niet in de tool-map zelf). De tool zoekt het bestand omhoog vanaf de werkmap totdat de repository root is bereikt.

### Opbouw

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

### Velden

| Veld | Type | Verplicht | Beschrijving |
|---|---|---|---|
| `languagefileextension` | `string` | Ja | Bestandsextensie (zonder punt) van de bronbestanden die gedocumenteerd moeten worden. Bijvoorbeeld `cs` voor C#, `ts` voor TypeScript. |
| `exclude` | `string[]` | Nee | Lijst van [glob-patronen](https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing) voor bestanden die **niet** gedocumenteerd moeten worden. Paden zijn relatief t.o.v. de repository root. |

### Voorbeelden van exclude-patronen

| Patroon | Effect |
|---|---|
| `**/*.Designer.cs` | Sluit alle gegenereerde Designer-bestanden uit |
| `tests/**` | Sluit de volledige testmap uit |
| `**/Migrations/**` | Sluit alle EF Core migratiemappen uit |
| `src/Generated/**` | Sluit een specifieke gegenereerde map uit |

> De tool sluit de eigen `tools/`-map altijd automatisch uit, ongeacht de inhoud van `docsettings.json`.

---

## De agent opzetten op Azure AI Foundry

De tool maakt gebruik van de Azure OpenAI-service via Azure AI Foundry. Volg de onderstaande stappen om een model te deployen en de benodigde gegevens op te halen.

### 1. Azure AI Foundry-project aanmaken

1. Ga naar [ai.azure.com](https://ai.azure.com) en log in.
2. Klik op **+ Nieuw project** en doorloop de wizard.
3. Koppel het project aan een Azure-abonnement en resourcegroep.

### 2. Een model deployen

1. Open je project in Azure AI Foundry.
2. Ga naar **Deployments** → **+ Deploy model** → **Deploy base model**.
3. Selecteer een geschikt model, bijvoorbeeld **gpt-4o-mini** of **gpt-4.0**.
4. Geef de deployment een naam (dit wordt de `DeploymentName` in de configuratie).
5. Kies het gewenste quota en klik op **Deploy**.

> De tool gebruikt API-versie `2024-10-21`. Zorg dat het gekozen model deze versie ondersteunt.

### 3. Endpoint en API-sleutel ophalen

1. Ga in je Azure AI Foundry-project naar **Deployments** en open de deployment die je zojuist hebt aangemaakt.
2. Klik op **View code** of navigeer naar de projectoverzichtspagina.
3. Kopieer de volgende waarden:

| Waarde | Waar te vinden |
|---|---|
| **Endpoint** | Projectoverzicht → *Azure OpenAI endpoint* (formaat: `https://<naam>.openai.azure.com/`) |
| **API Key** | Projectoverzicht → *Keys and Endpoint* → Key 1 of Key 2 |
| **Deployment Name** | De naam die je in stap 2 hebt opgegeven |

### 4. Configuratie koppelen aan de tool

Gebruik de waarden uit stap 3 om de configuratie in te stellen zoals beschreven in [sectie 2](#2-configuratiewaarden-instellen).


---

## Uitvoeren via GitHub Actions

De tool kan automatisch draaien na elke push naar een branch. De workflow zorgt ervoor dat documentatie gegenereerd wordt voor alle bestanden die in de betreffende commit zijn gewijzigd.

### 1. Secrets instellen in GitHub

Ga naar je repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret** en voeg de volgende secrets toe:

| Secret | Waarde |
|---|---|
| `OPENAI_APIKEY` | De API-sleutel van je Azure AI Foundry-deployment |
| `AZURE_OPENAI_ENDPOINT` | Het endpoint, bijv. `https://<naam>.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENTNAME` | De naam van de model-deployment |

### 2. Workflow-bestand aanmaken

Maak het bestand `.github/workflows/pipeline.yml` aan in de root van je repository met de volgende inhoud:

```yaml
name: Generate AI Documentation

on:
  push:
    branches:
      - '**'  # draait op elke push, op elke branch

jobs:
  build-and-run:
    runs-on: ubuntu-latest
    permissions:
      contents: write       # vereist om een nieuwe branch aan te maken en te pushen
      pull-requests: write  # vereist om een pull request aan te maken via de gh CLI
    env:
      FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true  # zorgt dat JS-based actions draaien op Node.js 24

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2  # haalt HEAD én HEAD~1 op; de tool heeft beide commits nodig om gewijzigde bestanden te bepalen

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Configure Git
        run: |
          # stel de identiteit in die git gebruikt voor de automatische commit
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          # koppel het GITHUB_TOKEN aan de remote URL zodat git push werkt zonder SSH-sleutel
          git remote set-url origin https://x-access-token:${{ secrets.GITHUB_TOKEN }}@github.com/${{ github.repository }}

      - name: Run AI Doc Tool
        env:
          AzureOpenAI__DeploymentName: ${{ secrets.AZURE_OPENAI_DEPLOYMENTNAME }}  # naam van de model-deployment in Foundry
          AzureOpenAI__Endpoint:       ${{ secrets.AZURE_OPENAI_ENDPOINT }}         # Azure OpenAI endpoint URL
          AzureOpenAI__ApiKey:         ${{ secrets.OPENAI_APIKEY }}                 # API-sleutel voor authenticatie
          Documentation__BasePath:     docs/generated                               # map waar de markdown-bestanden naartoe worden geschreven
          GH_TOKEN:                    ${{ secrets.GITHUB_TOKEN }}                  # gebruikt door de gh CLI om het pull request aan te maken
        run: |
          # start de tool via het .csproj-pad zodat dotnet run het juiste project vindt
          dotnet run --project tools/documentationAutomationv1/src/Presentation/documentationAutomationv1.Presentation.csproj
```

### 3. Toelichting op de workflow

| Instelling | Reden |
|---|---|
| `fetch-depth: 2` | De tool vergelijkt `HEAD~1..HEAD` om gewijzigde bestanden te vinden. Zonder deze instelling is de Git-geschiedenis te ondiep. |
| `permissions: contents: write` | Vereist voor het aanmaken van de documentatiebranch en het pushen van de commit. |
| `permissions: pull-requests: write` | Vereist zodat de GitHub CLI (`gh pr create`) een pull request kan openen. |
| `git remote set-url` | Koppelt het `GITHUB_TOKEN` aan de remote URL zodat `git push` werkt zonder aparte SSH-sleutel. |
| `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24` | Zorgt dat JavaScript-based actions draaien op Node.js 24 in plaats van een verouderde versie. |
| `GH_TOKEN` | De `gh` CLI gebruikt dit token automatisch. `secrets.GITHUB_TOKEN` is beschikbaar in elke GitHub Actions-workflow zonder extra configuratie. |
| `Documentation__BasePath` | Schrijft de gegenereerde documentatie naar `docs/generated/` in de repository. |
| `--project <pad>` | Geeft het exacte project op zodat `dotnet run` het juiste startproject vindt, ongeacht de werkmap.

---
