param(
    [ValidateSet('add', 'update', 'remove')]
    [string]$Action,

    [string]$Name,

    [string]$Project = 'Polisabroso.App',

    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRegistry = @{
    'Polisabroso.App' = @{
        Project = 'src/Polisabroso.App/Polisabroso.App.csproj'
        StartupProject = 'src/Polisabroso.App/Polisabroso.App.csproj'
        MigrationsDirectory = 'Persistence/Migrations'
        Configuration = 'Release'
    }
}

function Invoke-Ef {
    param([string[]]$Arguments, [switch]$Capture)

    if ($Capture) {
        $output = & dotnet ef @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Could not list migrations:`n$($output -join [Environment]::NewLine)"
        }
        return $output
    }

    & dotnet ef @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "The EF Core command exited with code $LASTEXITCODE."
    }
}

function Format-CommandArgument {
    param([string]$Value)

    return "'$(($Value -replace "'", "''"))'"
}

function Confirm-ClipboardCopy {
    param([string[]]$EfArguments, [string[]]$ScriptArguments)

    $answer = (Read-Host 'Copy the EF and script commands to the clipboard? (y/N)').Trim()
    if ($answer -notin @('y', 'yes')) {
        return
    }

    $efCommand = 'dotnet ef ' + (($EfArguments | ForEach-Object { Format-CommandArgument $_ }) -join ' ')
    $scriptCommand = '.\scripts\migrations.ps1'
    for ($i = 0; $i -lt $ScriptArguments.Count; $i += 2) {
        $scriptCommand += " $($ScriptArguments[$i]) $(Format-CommandArgument $ScriptArguments[$i + 1])"
    }
    Set-Clipboard -Value ($efCommand + [Environment]::NewLine + $scriptCommand)
    Write-Host 'Both commands were copied to the clipboard.'
}

try {
    if ([string]::IsNullOrWhiteSpace($Action)) {
        do {
            $Action = (Read-Host 'Action (add, update, or remove)').Trim().ToLowerInvariant()
        } until ($Action -in @('add', 'update', 'remove'))
    }

    if ($Action -eq 'add' -and [string]::IsNullOrWhiteSpace($Name)) {
        do {
            $Name = (Read-Host 'Name of the new migration').Trim()
        } until (-not [string]::IsNullOrWhiteSpace($Name))
    }

    $projectConfig = $projectRegistry[$Project]
    if ($null -eq $projectConfig) {
        $candidatePath = if ([IO.Path]::IsPathRooted($Project)) { $Project } else { Join-Path $repoRoot $Project }
        $candidatePath = [IO.Path]::GetFullPath($candidatePath)
        foreach ($registeredProject in $projectRegistry.Values) {
            $registeredPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $registeredProject.Project))
            if ($candidatePath -eq $registeredPath -or $candidatePath -eq (Split-Path -Parent $registeredPath)) {
                $projectConfig = $registeredProject
                break
            }
        }
    }
    if ($null -eq $projectConfig) {
        $projectConfig = @{
            Project = $Project
            StartupProject = $Project
            MigrationsDirectory = 'Migrations'
            Configuration = 'Release'
        }
    }

    $projectPath = if ([IO.Path]::IsPathRooted($projectConfig.Project)) { $projectConfig.Project } else { Join-Path $repoRoot $projectConfig.Project }
    if (Test-Path -LiteralPath $projectPath -PathType Container) {
        $projectFiles = @(Get-ChildItem -LiteralPath $projectPath -Filter '*.csproj' -File)
        if ($projectFiles.Count -ne 1) {
            throw "The directory '$projectPath' must contain exactly one .csproj file."
        }
        $projectPath = $projectFiles[0].FullName
    }
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf) -or [IO.Path]::GetExtension($projectPath) -ne '.csproj') {
        throw "No .csproj project was found at '$projectPath'."
    }

    $startupPath = if ([IO.Path]::IsPathRooted($projectConfig.StartupProject)) {
        $projectConfig.StartupProject
    } else {
        Join-Path $repoRoot $projectConfig.StartupProject
    }
    if (Test-Path -LiteralPath $startupPath -PathType Container) {
        $startupFiles = @(Get-ChildItem -LiteralPath $startupPath -Filter '*.csproj' -File)
        if ($startupFiles.Count -ne 1) {
            throw "The startup directory '$startupPath' must contain exactly one .csproj file."
        }
        $startupPath = $startupFiles[0].FullName
    }
    if (-not (Test-Path -LiteralPath $startupPath -PathType Leaf) -or [IO.Path]::GetExtension($startupPath) -ne '.csproj') {
        throw "No startup .csproj project was found at '$startupPath'."
    }

    $commonArgs = @('--project', $projectPath, '--startup-project', $startupPath, '--configuration', $projectConfig.Configuration)
    $scriptArgs = @('-Action', $Action, '-Project', $Project)

    Push-Location $repoRoot
    try {
        switch ($Action) {
            'add' {
                $commandArgs = @('migrations', 'add', $Name, '--output-dir', $projectConfig.MigrationsDirectory) + $commonArgs
                $scriptArgs += @('-Name', $Name)
            }
            'update' {
                if ([string]::IsNullOrWhiteSpace($Name)) {
                    $rawList = @(Invoke-Ef -Arguments (@('migrations', 'list', '--json', '--no-connect') + $commonArgs) -Capture)
                    $json = ($rawList -join [Environment]::NewLine)
                    $jsonStart = $json.IndexOf('[')
                    $jsonEnd = $json.LastIndexOf(']')
                    if ($jsonStart -lt 0 -or $jsonEnd -lt $jsonStart) {
                        throw "EF Core did not return a valid migration list:`n$json"
                    }
                    $migrationJson = $json.Substring($jsonStart, $jsonEnd - $jsonStart + 1)
                    if ($migrationJson.Trim() -eq '[]') {
                        throw 'The project has no migrations to apply.'
                    }
                    $migrations = @($migrationJson | ConvertFrom-Json)
                    if ($migrations.Count -eq 0) {
                        throw 'The project has no migrations to apply.'
                    }

                    Write-Host 'Available migrations:'
                    for ($i = 0; $i -lt $migrations.Count; $i++) {
                        Write-Host "  $($i + 1). $($migrations[$i].name)"
                    }
                    $latest = $migrations.Count
                    do {
                        $selection = Read-Host "Select a migration (Enter = $latest, the latest)"
                        if ([string]::IsNullOrWhiteSpace($selection)) { $selection = [string]$latest }
                        $selectedNumber = 0
                        $validSelection = [int]::TryParse($selection, [ref]$selectedNumber) -and
                            $selectedNumber -ge 1 -and $selectedNumber -le $migrations.Count
                    } until ($validSelection)
                    $Name = $migrations[$selectedNumber - 1].id
                }

                $commandArgs = @('database', 'update', $Name) + $commonArgs
                if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
                    $commandArgs += @('--connection', $ConnectionString)
                }
                $scriptArgs += @('-Name', $Name)
                if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
                    $scriptArgs += @('-ConnectionString', $ConnectionString)
                }
            }
            'remove' {
                $commandArgs = @('migrations', 'remove') + $commonArgs
            }
        }
        Confirm-ClipboardCopy -EfArguments $commandArgs -ScriptArguments $scriptArgs
        Invoke-Ef -Arguments $commandArgs
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
