# SonarQube Cloud Code Coverage Architecture

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Developer Workflow                            │
└─────────────────────────────────────────────────────────────────┘

Step 1: LOCAL TESTING
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│  Developer runs:  ./scripts/test-with-coverage.sh               │
│                                                                  │
│  ├─ Restores NuGet dependencies                                 │
│  ├─ Builds solution (Release)                                   │
│  ├─ Runs: dotnet test --collect:"XPlat Code Coverage"          │
│  │         --settings ".runsettings"                            │
│  └─ Generates: coverage.opencover.xml                           │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Generates coverage report
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│  Local Coverage Report (coverage.opencover.xml)                 │
│  Location: */bin/Release/net10.0/coverage.opencover.xml        │
│                                                                  │
│  Can be reviewed locally before commit                          │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Developer commits & pushes
                              ▼

┌─────────────────────────────────────────────────────────────────┐
│              Step 2: GITHUB ACTIONS WORKFLOW                    │
│           (.github/workflows/sonarqube-cloud.yml)               │
└─────────────────────────────────────────────────────────────────┘

    ┌─────────────────────────────────────────────┐
    │ Trigger: push/PR to main or develop         │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 1. Checkout Code (with full history)        │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 2. Setup .NET 10 Runtime                    │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 3. Restore Dependencies                     │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 4. Build Solution (Release)                 │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────────────────────┐
    │ 5. Run Tests with Coverage                                  │
    │    - Uses: .runsettings (OpenCover format)                  │
    │    - Generates: coverage.opencover.xml                      │
    │    - Excludes: Test projects, AppHost, etc.                 │
    └─────────────────────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 6. Install SonarQube Scanner                │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌────────────────────────────────────────────────────────────┐
    │ 7. Begin SonarQube Analysis                                │
    │    dotnet sonarscanner begin \                             │
    │      /k:PROJECT_KEY \                                      │
    │      /o:ORGANIZATION \                                     │
    │      /d:sonar.host.url=https://sonarcloud.io \            │
    │      /d:sonar.login=SONAR_TOKEN \                         │
    │      /d:sonar.cs.opencover.reportsPaths=*.xml            │
    └────────────────────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 8. Build for Analysis (second pass)         │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 9. End Analysis & Upload Results            │
    │    dotnet sonarscanner end /d:sonar.login   │
    └─────────────────────────────────────────────┘
              │
              ▼
    ┌─────────────────────────────────────────────┐
    │ 10. Upload Artifacts (test results, reports)│
    └─────────────────────────────────────────────┘
              │
              ▼

┌─────────────────────────────────────────────────────────────────┐
│              Step 3: SONARQUBE CLOUD ANALYSIS                   │
│                 https://sonarcloud.io                           │
└─────────────────────────────────────────────────────────────────┘

              │
              │ Receives analysis from GitHub Actions
              │
              ▼
    ┌──────────────────────────────────────────────┐
    │ SonarQube Cloud Project                      │
    │                                              │
    │ ├─ Project Key: AspireKeyCloakTemplate      │
    │ ├─ Organization: your-org-key                │
    │ └─ Configuration: sonar-project.properties   │
    └──────────────────────────────────────────────┘
              │
              ▼
    ┌──────────────────────────────────────────────┐
    │ Analysis & Coverage Processing               │
    │                                              │
    │ ├─ Parses: coverage.opencover.xml            │
    │ ├─ Applies: sonar-project.properties         │
    │ ├─ Calculates: Coverage metrics              │
    │ ├─ Checks: Quality gates                     │
    │ └─ Generates: Issue reports                  │
    └──────────────────────────────────────────────┘
              │
              ▼
    ┌──────────────────────────────────────────────┐
    │ Results & Feedback                           │
    │                                              │
    │ ├─ Project Dashboard                        │
    │ ├─ GitHub PR Comments                       │
    │ ├─ Coverage Reports                         │
    │ └─ Quality Gate Status                      │
    └──────────────────────────────────────────────┘
              │
              ▼

┌─────────────────────────────────────────────────────────────────┐
│                 Developer Sees Results                           │
│                                                                 │
│ 1. GitHub Actions workflow status (success/failure)            │
│ 2. SonarQube Cloud dashboard (detailed analysis)               │
│ 3. PR comments with quality gate status                        │
│ 4. Coverage trends over time                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration Files Overview

```
AspireKeyCloakTemplate (Root)
│
├── sonar-project.properties
│   └─ SonarQube analysis configuration
│      • Project identification
│      • Coverage report paths
│      • Code exclusions
│      • Language settings
│
├── .github/workflows/
│   └─ sonarqube-cloud.yml
│      └─ CI/CD automation for analysis
│
├── src/AspireKeyCloakTemplate.Gateway.UnitTests/
│   └─ .runsettings
│      └─ Coverlet configuration
│         • OpenCover format
│         • Coverage exclusions
│
├── scripts/
│   ├─ test-with-coverage.sh (Unix)
│   └─ test-with-coverage.ps1 (Windows)
│      └─ Local testing scripts
│
└── Documentation/
    ├─ SONARQUBE_SETUP.md (comprehensive guide)
    ├─ CODE_COVERAGE_QUICKREF.md (quick reference)
    └─ SONARQUBE_IMPLEMENTATION.md (this implementation)
```

## Key Technologies

```
┌──────────────────────────────────────────────────────┐
│  Test Execution (Local & CI)                        │
│  ├─ Framework: xUnit 2.9.3                          │
│  ├─ Test SDK: Microsoft.NET.Test.Sdk 18.0.1         │
│  └─ Target: net10.0                                 │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  Coverage Collection                                │
│  ├─ Tool: coverlet.collector 6.0.4                  │
│  ├─ Format: OpenCover (XML)                         │
│  └─ Collection: XPlat Code Coverage                 │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  Code Analysis                                       │
│  ├─ Scanner: dotnet-sonarscanner (latest)           │
│  ├─ Platform: SonarQube Cloud                       │
│  └─ Integration: GitHub Actions                     │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  CI/CD Pipeline                                     │
│  ├─ Platform: GitHub Actions                        │
│  ├─ Trigger: Push + Pull Requests                   │
│  ├─ Runtime: Ubuntu latest                          │
│  └─ Configuration: .github/workflows/                │
└──────────────────────────────────────────────────────┘
```

## Coverage Metrics Flow

```
Source Code                Coverage Collection        Analysis & Reporting
──────────────            ────────────────────       ───────────────────

API/                  ┐                         ┐
Gateway/              │ xUnit Tests             │ coverlet          ┐
Services/             │ (.runsettings)          │ (OpenCover XML)   │
Models/       ───────▶│ Coverage Data ─────────▶│ coverage.open-    │
                      │                         │ cover.xml         │ SonarQube
Logic/                │ • Statements            │                   │ Cloud
                      │ • Branches              │ Metrics:          │ ────────▶
                      │ • Conditions            │ • Line Coverage   │
                      │                         │ • Branch Cov.     │ Dashboard
                      │ Excludes:               │ • Complexity      │ & Reports
                      │ • *.UnitTests.*         │                   │
Excluded from         │ • AppHost               │ Quality Gates:    │
Coverage:             │ • ServiceDefaults       │ • Coverage %      │
────────────          │                         │ • Issues          │
                      └─────────────────────────┘ • Ratings         │
• Test Projects                                                    │
• AppHost                                                          │
• Infrastructure     └──────────────────────────────────────────────┘
  Code
```

## Setup Checklist

```
☐ Review this document and understand the flow
☐ Create SonarQube Cloud project (sonarcloud.io)
☐ Note Organization Key and Project Key
☐ Generate SonarQube Token
☐ Add GitHub Secrets (SONAR_TOKEN, SONAR_ORGANIZATION, etc.)
☐ Update sonar-project.properties with your org/project keys
☐ Commit all files and push to repository
☐ Verify GitHub Actions workflow runs successfully
☐ Check SonarQube Cloud dashboard for results
☐ Configure Quality Gates in SonarQube Cloud
☐ (Optional) Set up GitHub branch protection based on quality gates
☐ Share documentation with team members
```

For more details, see `SONARQUBE_SETUP.md`.

