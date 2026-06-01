# documentationAutomationv1

Een CLI-tool die automatisch Markdown-documentatie genereert voor gewijzigde bronbestanden in een Git-repository. De tool gebruikt Azure AI Foundry (Azure OpenAI) om verschillende soorten documentaties te genereren.

---

## Inhoudsopgave

1. [Vereisten](#vereisten)
2. [De tool in je project zetten](#de-tool-in-je-project-zetten)
3. [Een AI-model opzetten op Azure AI Foundry](#een-ai-model-opzetten-op-azure-ai-foundry)
4. [Configuratie instellen](#configuratie-instellen)
5. [Uitvoeren via GitHub Actions](#uitvoeren-via-github-actions)

---

## Vereisten

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [GitHub CLI (`gh`)](https://cli.github.com/) — ingelogd op de juiste organisatie/repository
- Een Azure AI Foundry-project met een gedeployde model (zie [sectie 3](#een-ai-model-opzetten-op-azure-ai-foundry))

---

## De tool in je project zetten

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

Zodra de tool draait, voert hij automatisch de volgende stappen uit:

1. Laadt `docsettings.json` vanuit de repository root.
2. Maakt een tijdelijke documentatiebranch aan: `docs/<huidige-branch>-<tijdstempel>`.
3. Haalt alle gewijzigde bestanden op t.o.v. de vorige commit (`HEAD~1..HEAD`).
4. Filtert bestanden op extensie en de uitsluitingspatronen uit `docsettings.json`.
5. Genereert Markdown-documentatie via Azure AI Foundry.
6. Schrijft de documentatie naar `docs/generated/` op de documentatiebranch.
7. Commit en pusht de documentatiebranch.
8. Maakt een pull request aan via de GitHub CLI.

---

## Een AI-model opzetten op Azure AI Foundry

De tool maakt gebruik van de Azure OpenAI-service via Azure AI Foundry. Volg de onderstaande stappen om alles vanaf het begin op te zetten: van het aanmaken van de Azure-resources tot het deployen van een model en het ophalen van de benodigde gegevens.

### 1. Resource Group aanmaken

1. Ga naar [portal.azure.com](https://portal.azure.com) en log in.
2. Zoek in de zoekbalk bovenaan naar **Resource groups** en klik erop.
3. Klik op **+ Create**.
4. Kies je **abonnement**, geef de resourcegroep een naam (bijv. `rg-documentatie-tool`) en kies een **regio** die dicht bij je gebruikers staat.
5. Klik op **Review + create** → **Create**.

### 2. Azure OpenAI resource aanmaken

1. Ga naar [portal.azure.com](https://portal.azure.com).
2. Zoek in de zoekbalk naar **Azure OpenAI** en klik op **Azure OpenAI** onder *Services*.
3. Klik op **+ Create**.
4. Vul de volgende velden in:

   | Veld | Waarde |
   |---|---|
   | **Subscription** | Jouw Azure-abonnement |
   | **Resource group** | De resourcegroep die je in stap 1 hebt aangemaakt |
   | **Region** | Kies een regio die het gewenste model ondersteunt (zie [modelbeschikbaarheid](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability)) |
   | **Name** | Een unieke naam — dit wordt onderdeel van het endpoint: `https://<naam>.openai.azure.com/` |
   | **Pricing tier** | Standard S0 |

5. Klik op **Review + create** → **Create**.
6. Wacht tot de deployment voltooid is en klik op **Go to resource**.

### 3. Azure AI Foundry-project aanmaken

1. Ga naar [ai.azure.com](https://ai.azure.com) en log in.
2. Klik op **+ Nieuw project** en doorloop de wizard.
3. Koppel het project aan het **abonnement** en de **resourcegroep** die je in de vorige stappen hebt aangemaakt.

> Azure AI Foundry maakt binnen de resourcegroep automatisch een hub aan als die er nog niet is.

### 4. Een model deployen

1. Open je project in Azure AI Foundry.
2. Ga naar **Deployments** → **+ Deploy model** → **Deploy base model**.
3. Selecteer een geschikt model, bijvoorbeeld **gpt-4o-mini** of **gpt-4o**.
4. Geef de deployment een naam (dit wordt de `DeploymentName` in de configuratie).
5. Kies het gewenste quota en klik op **Deploy**.

> De tool gebruikt API-versie `2024-10-21`. Zorg dat het gekozen model deze versie ondersteunt.

### 5. Endpoint en API-sleutel ophalen

De benodigde waarden zijn te vinden in de Azure portal, bij de Azure OpenAI resource die je in stap 2 hebt aangemaakt:

1. Ga naar [portal.azure.com](https://portal.azure.com) en open de Azure foundry resource.
2. Klik in het linker menu op **Keys and Endpoint**.
3. Kopieer de volgende waarden — je hebt ze nodig in de volgende stap:
4. de deployment name vindt je als je naar foundry portal gaat. rechts boven zie je operate, nagigeer daar heen vervolgens zie je links assets. klik daarop en dan zie models staan en als je daarop klitk zie je de deployment namen van je models.

| Waarde | Waar te vinden |
|---|---|
| **Endpoint** | *Endpoint* — formaat: `https://<naam>.openai.azure.com/` |
| **API Key** | *Key 1* of *Key 2* |
| **Deployment Name** | De naam die je in stap 4 hebt opgegeven |

---

## Configuratie instellen

### docsettings.json

Het bestand `docsettings.json` bepaalt welke bestanden de tool documenteert. Plaats het in de **root van de repository** die je wilt documenteren (naast de `.git`-map, dus niet in de tool-map zelf). De tool zoekt het bestand omhoog vanaf de werkmap totdat de repository root is bereikt.

#### Opbouw

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

#### Velden

| Veld | Type | Verplicht | Beschrijving |
|---|---|---|---|
| `languagefileextension` | `string` | Ja | Bestandsextensie (zonder punt) van de bronbestanden die gedocumenteerd moeten worden. Bijvoorbeeld `cs` voor C#, `ts` voor TypeScript. |
| `exclude` | `string[]` | Nee | Lijst van [glob-patronen](https://learn.microsoft.com/en-us/dotnet/core/extensions/file-globbing) voor bestanden die **niet** gedocumenteerd moeten worden. Paden zijn relatief t.o.v. de repository root. |

#### Voorbeelden van exclude-patronen

| Patroon | Effect |
|---|---|
| `**/*.Designer.cs` | Sluit alle gegenereerde Designer-bestanden uit |
| `tests/**` | Sluit de volledige testmap uit |
| `**/Migrations/**` | Sluit alle EF Core migratiemappen uit |
| `src/Generated/**` | Sluit een specifieke gegenereerde map uit |

> De tool sluit de eigen `tools/`-map altijd automatisch uit, ongeacht de inhoud van `docsettings.json`.

### GitHub Secrets aanmaken

Gebruik de waarden die je in [sectie 3, stap 5](#5-endpoint-en-api-sleutel-ophalen) hebt opgehaald om drie secrets aan te maken in GitHub. Ga naar je repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Naam (in GitHub) | Waarde |
|---|---|
| `OPENAI_APIKEY` | De API-sleutel van je Azure AI Foundry-deployment |
| `AZURE_OPENAI_ENDPOINT` | Het endpoint, bijv. `https://<naam>.openai.azure.com/` |
| `AZURE_OPENAI_DEPLOYMENTNAME` | De naam van de model-deployment |

### Configuratiewaarden koppelen in de workflow

De namen van de omgevingsvariabelen volgen de .NET-conventie waarbij een dubbele underscore (`__`) een niveaugrens aangeeft in de configuratiehiërarchie. `AzureOpenAI__ApiKey` staat dus gelijk aan de configuratiesleutel `AzureOpenAI:ApiKey` in code.

| Omgevingsvariabele | Verplicht | Waarde |
|---|---|---|
| `AzureOpenAI__ApiKey` | Ja | De API-sleutel van je Azure AI Foundry-deployment |
| `AzureOpenAI__Endpoint` | Ja | Het endpoint, bijv. `https://<naam>.openai.azure.com/` |
| `AzureOpenAI__DeploymentName` | Ja | De naam van de model-deployment |
| `Documentation__BasePath` | Nee | Pad waar documentatie naartoe geschreven wordt (standaard: `docs`) |

In het workflow-bestand koppel je de GitHub Secrets via de `env:`-sectie aan de omgevingsvariabelen die de tool verwacht:

```yaml
env:
  AzureOpenAI__ApiKey:         ${{ secrets.OPENAI_APIKEY }}
  AzureOpenAI__Endpoint:       ${{ secrets.AZURE_OPENAI_ENDPOINT }}
  AzureOpenAI__DeploymentName: ${{ secrets.AZURE_OPENAI_DEPLOYMENTNAME }}
```

GitHub injecteert de waarden van de secrets op het moment dat de workflow draait. De tool leest ze vervolgens als gewone omgevingsvariabelen via de .NET-configuratieprovider.

---

## Uitvoeren via GitHub Actions

De tool kan automatisch draaien na elke push naar een branch. De workflow zorgt ervoor dat documentatie gegenereerd wordt voor alle bestanden die in de betreffende commit zijn gewijzigd.

### 1. Workflow-bestand aanmaken

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

### 2. Toelichting op de workflow

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

### 3. Gedrag bij fouten in de pipeline

Als de tool een fout tegenkomt (bijvoorbeeld een ontbrekende configuratiewaarde of een mislukt git-commando), stopt de tool met een non-zero exit code. Standaard markeert GitHub Actions de huidige job dan als gefaald en worden alle volgende stappen in die job overgeslagen.

Er zijn twee manieren om hiermee om te gaan, afhankelijk van je situatie:

**Optie 1 — Pipeline laten falen (standaard)**

De workflow uit [sectie 2](#2-workflow-bestand-aanmaken) is al geconfigureerd volgens dit gedrag. Als de tool faalt, faalt de job en worden alle volgende stappen overgeslagen. Dit zorgt ervoor dat een mislukte documentatiestap direct zichtbaar is in het pipeline-overzicht.

**Optie 2 — Documentatie en andere jobs onafhankelijk van elkaar**

Als je pipeline andere jobs bevat die niets te maken hebben met documentatie (zoals een deployment), is het beter om de documentatietool in een **aparte job** te zetten zonder dat andere jobs daarvan afhangen. Zo loopt de deployment gewoon door, maar is in het pipeline-overzicht nog steeds duidelijk zichtbaar welke job is mislukt.

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
    # geen 'needs: generate-docs' → start parallel aan generate-docs, ongeacht de uitkomst daarvan
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Build
        run: dotnet build
      - name: Deploy
        run: echo "Deployment stap hier"
```

> Gebruik `continue-on-error: true` op een stap alleen als je de fout bewust wilt negeren. Het nadeel hiervan is dat de job als geheel groen wordt gemarkeerd, ook als de documentatiestap is mislukt.

---
