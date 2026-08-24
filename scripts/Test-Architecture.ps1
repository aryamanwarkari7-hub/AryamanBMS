# Validates project-reference direction.
# This script only reads .csproj files; it does not edit files or Git state.

$repositoryRoot = Split-Path -Parent $PSScriptRoot

$projectPaths = @{
    Web          = Join-Path $repositoryRoot "AryamanBMS\AryamanBMS.csproj"
    Business     = Join-Path $repositoryRoot "AryamanBMS.Business\AryamanBMS.Business.csproj"
    Repositories = Join-Path $repositoryRoot "AryamanBMS.Repositories\AryamanBMS.Repositories.csproj"
    Database     = Join-Path $repositoryRoot "AryamanBMS.Database\AryamanBMS.Database.csproj"
    Models       = Join-Path $repositoryRoot "AryamanBMS.Models\AryamanBMS.Models.csproj"
    Utilities    = Join-Path $repositoryRoot "AryamanBMS.Utilities\AryamanBMS.Utilities.csproj"
}

$allowedReferences = @{
    Web          = @("Business", "Repositories", "Database", "Models", "Utilities")
    Business     = @("Repositories", "Models", "Utilities")
    Repositories = @("Database", "Models", "Utilities")
    Database     = @("Models", "Utilities")
    Models       = @()
    Utilities    = @()
}

$pathToProjectName = @{}

foreach ($projectName in $projectPaths.Keys) {
    $fullPath = [System.IO.Path]::GetFullPath($projectPaths[$projectName])
    $pathToProjectName[$fullPath] = $projectName
}

$violations = @()

foreach ($projectName in $projectPaths.Keys) {
    $projectPath = $projectPaths[$projectName]

    if (-not (Test-Path -LiteralPath $projectPath)) {
        $violations += "Missing project file: $projectPath"
        continue
    }

    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw

    $references = $projectXml.SelectNodes(
        "//*[local-name()='ProjectReference']")

    foreach ($reference in $references) {
        $includePath = $reference.GetAttribute("Include")

        $referencedPath = [System.IO.Path]::GetFullPath(
            [System.IO.Path]::Combine(
                (Split-Path -Parent $projectPath),
                $includePath))

        if (-not $pathToProjectName.ContainsKey($referencedPath)) {
            continue
        }

        $referencedProject = $pathToProjectName[$referencedPath]

        if ($allowedReferences[$projectName] -notcontains $referencedProject) {
            $violations +=
                "$projectName must not reference $referencedProject."
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Architecture dependency check failed:" -ForegroundColor Red

    $violations | ForEach-Object {
        Write-Host " - $_" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Architecture dependency check passed." -ForegroundColor Green
exit 0