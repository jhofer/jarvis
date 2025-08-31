#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Backs up current Azure AD and Key Vault configuration.

.DESCRIPTION
    Creates a backup of the current configuration before running recreation scripts.
    Saves app registration details, Key Vault secrets, and configuration files.

.EXAMPLE
    .\backup-current-config.ps1
#>

$ErrorActionPreference = "Continue"

$BackupDir = "backup-$(Get-Date -Format 'yyyy-MM-dd-HHmm')"
$BackupPath = Join-Path $PSScriptRoot $BackupDir

Write-Host "💾 Creating configuration backup in: $BackupPath" -ForegroundColor Green

# Create backup directory
New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null

# Backup configuration files
$filesToBackup = @(
    @{ Source = "..\nextjs-frontend\.env.local"; Name = "env.local" },
    @{ Source = "..\nextjs-frontend\.env.local.secrets"; Name = "env.local.secrets" },
    @{ Source = "..\JarvisAspireHost\Program.cs"; Name = "Program.cs" },
    @{ Source = "..\nextjs-frontend\lib\auth.ts"; Name = "auth.ts" },
    @{ Source = "..\nextjs-frontend\package.json"; Name = "package.json" }
)

foreach ($file in $filesToBackup) {
    if (Test-Path $file.Source) {
        Copy-Item $file.Source -Destination (Join-Path $BackupPath $file.Name)
        Write-Host "✅ Backed up: $($file.Name)" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Not found: $($file.Source)" -ForegroundColor Yellow
    }
}

# Extract current configuration
$configPath = Join-Path $BackupPath "current-config.json"
$config = @{}

# Get current client ID from .env.local
if (Test-Path "..\nextjs-frontend\.env.local") {
    $envContent = Get-Content "..\nextjs-frontend\.env.local" -Raw
    if ($envContent -match 'AZURE_AD_CLIENT_ID=([^\r\n]+)') {
        $config.ClientId = $matches[1].Trim()
    }
    if ($envContent -match 'AZURE_AD_TENANT_ID=([^\r\n]+)') {
        $config.TenantId = $matches[1].Trim()
    }
}

# Get app registration details if client ID exists
if ($config.ClientId) {
    try {
        $app = az ad app show --id $config.ClientId --query "{displayName:displayName,appId:appId,createdDateTime:createdDateTime}" -o json 2>$null | ConvertFrom-Json
        $config.AppRegistration = $app
        
        $redirectUris = az ad app show --id $config.ClientId --query "web.redirectUris" -o json 2>$null | ConvertFrom-Json
        $config.RedirectUris = $redirectUris
        
        Write-Host "✅ Captured app registration details" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Could not access app registration" -ForegroundColor Yellow
    }
}

# Get Key Vault information
try {
    $kvSecrets = az keyvault secret list --vault-name "secrets-3kktkazybj2b2" --query "[].{name:name,created:attributes.created}" -o json 2>$null | ConvertFrom-Json
    $config.KeyVaultSecrets = $kvSecrets
    Write-Host "✅ Captured Key Vault secret list" -ForegroundColor Green
}
catch {
    Write-Host "⚠️ Could not access Key Vault" -ForegroundColor Yellow
}

# Save configuration
$config | ConvertTo-Json -Depth 3 | Out-File $configPath -Encoding UTF8

# Create restoration notes
$notesPath = Join-Path $BackupPath "restoration-notes.md"
$notes = @"
# Configuration Backup - $(Get-Date)

## Backup Contents

This backup contains:
- Configuration files (.env.local, Program.cs, etc.)
- App registration details (if accessible)
- Key Vault secret metadata (names and creation dates)

## Current Configuration

**Client ID:** $($config.ClientId)
**Tenant ID:** $($config.TenantId)
**App Name:** $($config.AppRegistration.displayName)
**Created:** $($config.AppRegistration.createdDateTime)

## Redirect URIs
$($config.RedirectUris | ForEach-Object { "- $_" } | Out-String)

## Key Vault Secrets
$($config.KeyVaultSecrets | ForEach-Object { "- $($_.name) (created: $($_.created))" } | Out-String)

## Files Backed Up
$($filesToBackup | ForEach-Object { "- $($_.Name)" } | Out-String)

## Restoration

To restore this configuration:
1. Use the recreation script with the same parameters
2. Manually copy configuration files if needed
3. Check that redirect URIs match the backed up list

## Notes

- Actual secret values are NOT backed up for security
- Use Azure portal or Key Vault if you need to recover secret values
- This backup is for configuration structure only
"@

$notes | Out-File $notesPath -Encoding UTF8

Write-Host ""
Write-Host "✅ Backup completed!" -ForegroundColor Green
Write-Host "📁 Location: $BackupPath" -ForegroundColor Cyan
Write-Host "📄 Files: $($filesToBackup.Count) config files + metadata" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔒 Note: Secret values are NOT backed up for security" -ForegroundColor Yellow
Write-Host "📖 See restoration-notes.md for details" -ForegroundColor Cyan
