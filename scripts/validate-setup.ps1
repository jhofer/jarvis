#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates the current Azure AD and Key Vault setup.

.DESCRIPTION
    Checks if all components are properly configured:
    - Azure AD app registration exists and is accessible
    - Redirect URIs are configured correctly
    - Key Vault secrets exist and are accessible
    - Configuration files have correct values

.EXAMPLE
    .\validate-setup.ps1
#>

$ErrorActionPreference = "Continue"

Write-Host "🔍 Validating Azure AD and Key Vault Setup" -ForegroundColor Green
Write-Host ""

# Configuration
$KeyVaultName = "secrets-3kktkazybj2b2"
$LocalRedirectUri = "http://localhost:3000/api/auth/callback/azure-ad"
$ProdRedirectUri = "https://nextjs-frontend.whitepebble-d22a3c98.westeurope.azurecontainerapps.io/api/auth/callback/azure-ad"

$ValidationResults = @()

function Add-ValidationResult {
    param([string]$Test, [bool]$Passed, [string]$Message = "", [string]$Details = "")
    
    $status = if ($Passed) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    
    Write-Host "$status $Test" -ForegroundColor $color
    if ($Message) { Write-Host "   $Message" -ForegroundColor Gray }
    if ($Details) { Write-Host "   $Details" -ForegroundColor Yellow }
    
    $script:ValidationResults += [PSCustomObject]@{
        Test = $Test
        Passed = $Passed
        Message = $Message
        Details = $Details
    }
}

try {
    # Check Azure CLI login
    try {
        $account = az account show --query "user.name" -o tsv 2>$null
        Add-ValidationResult "Azure CLI Login" $true "Logged in as: $account"
    }
    catch {
        Add-ValidationResult "Azure CLI Login" $false "Not logged in" "Run: az login"
    }
    
    # Get client ID from .env.local
    $envLocalPath = "..\nextjs-frontend\.env.local"
    $clientId = $null
    if (Test-Path $envLocalPath) {
        $envContent = Get-Content $envLocalPath -Raw
        if ($envContent -match 'AZURE_AD_CLIENT_ID=([^\r\n]+)') {
            $clientId = $matches[1].Trim()
            Add-ValidationResult "Client ID in .env.local" $true "Found: $clientId"
        } else {
            Add-ValidationResult "Client ID in .env.local" $false "AZURE_AD_CLIENT_ID not found"
        }
    } else {
        Add-ValidationResult ".env.local file" $false "File not found" "Expected at: $envLocalPath"
    }
    
    # Check app registration exists
    if ($clientId) {
        try {
            $app = az ad app show --id $clientId --query "{displayName:displayName,appId:appId}" -o json 2>$null | ConvertFrom-Json
            Add-ValidationResult "App Registration Exists" $true "Found: $($app.displayName)"
            
            # Check redirect URIs
            $redirectUris = az ad app show --id $clientId --query "web.redirectUris" -o json 2>$null | ConvertFrom-Json
            $hasLocalUri = $redirectUris -contains $LocalRedirectUri
            $hasProdUri = $redirectUris -contains $ProdRedirectUri
            
            Add-ValidationResult "Local Redirect URI" $hasLocalUri "Expected: $LocalRedirectUri"
            Add-ValidationResult "Production Redirect URI" $hasProdUri "Expected: $ProdRedirectUri"
            
        }
        catch {
            Add-ValidationResult "App Registration Exists" $false "App registration not found or not accessible" "Client ID: $clientId"
        }
    }
    
    # Check Key Vault access
    try {
        $secrets = az keyvault secret list --vault-name $KeyVaultName --query "[].name" -o json 2>$null | ConvertFrom-Json
        Add-ValidationResult "Key Vault Access" $true "Can access vault: $KeyVaultName"
        
        # Check specific secrets
        $hasClientSecret = $secrets -contains "AZURE-AD-CLIENT-SECRET"
        $hasNextAuthSecret = $secrets -contains "NEXTAUTH-SECRET"
        
        Add-ValidationResult "Client Secret in Key Vault" $hasClientSecret "Secret: AZURE-AD-CLIENT-SECRET"
        Add-ValidationResult "NextAuth Secret in Key Vault" $hasNextAuthSecret "Secret: NEXTAUTH-SECRET"
        
        # Try to retrieve secrets (just test access, don't display values)
        if ($hasClientSecret) {
            try {
                az keyvault secret show --vault-name $KeyVaultName --name "AZURE-AD-CLIENT-SECRET" --query "value" -o tsv 2>$null | Out-Null
                Add-ValidationResult "Can Read Client Secret" $true "Secret accessible"
            }
            catch {
                Add-ValidationResult "Can Read Client Secret" $false "Permission denied"
            }
        }
        
    }
    catch {
        Add-ValidationResult "Key Vault Access" $false "Cannot access Key Vault" "Vault: $KeyVaultName"
    }
    
    # Check Program.cs configuration
    $programCsPath = "..\JarvisAspireHost\Program.cs"
    if (Test-Path $programCsPath) {
        $programContent = Get-Content $programCsPath -Raw
        
        $hasClientIdConfig = $programContent -match 'WithEnvironment\("AZURE_AD_CLIENT_ID"'
        $hasSecretConfig = $programContent -match 'WithEnvironment\("AZURE_AD_CLIENT_SECRET".*Configuration'
        $hasNextAuthConfig = $programContent -match 'WithEnvironment\("NEXTAUTH_SECRET".*Configuration'
        
        Add-ValidationResult "Program.cs Client ID Config" $hasClientIdConfig
        Add-ValidationResult "Program.cs Secret Config" $hasSecretConfig "Uses Configuration pattern"
        Add-ValidationResult "Program.cs NextAuth Config" $hasNextAuthConfig "Uses Configuration pattern"
    } else {
        Add-ValidationResult "Program.cs File" $false "File not found" "Expected at: $programCsPath"
    }
    
    # Check .env.local.secrets file (should exist for local dev)
    $envSecretsPath = "..\nextjs-frontend\.env.local.secrets"
    if (Test-Path $envSecretsPath) {
        Add-ValidationResult ".env.local.secrets file" $true "File exists for local development"
    } else {
        Add-ValidationResult ".env.local.secrets file" $false "File not found" "Create with: .\load-secrets.ps1"
    }
    
    # Check if Next.js packages are installed
    $packageJsonPath = "..\nextjs-frontend\package.json"
    $nodeModulesPath = "..\nextjs-frontend\node_modules"
    if (Test-Path $packageJsonPath -and Test-Path $nodeModulesPath) {
        Add-ValidationResult "Next.js Dependencies" $true "Node modules installed"
    } else {
        Add-ValidationResult "Next.js Dependencies" $false "Dependencies not installed" "Run: npm install"
    }
    
}
catch {
    Write-Host "❌ Validation error: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host ""
Write-Host "📊 Validation Summary" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

$passedTests = ($ValidationResults | Where-Object { $_.Passed }).Count
$totalTests = $ValidationResults.Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red

if ($failedTests -gt 0) {
    Write-Host ""
    Write-Host "❌ Failed Tests:" -ForegroundColor Red
    $ValidationResults | Where-Object { -not $_.Passed } | ForEach-Object {
        Write-Host "   • $($_.Test)" -ForegroundColor Red
        if ($_.Details) { Write-Host "     Fix: $($_.Details)" -ForegroundColor Yellow }
    }
    
    Write-Host ""
    Write-Host "🔧 Suggested Actions:" -ForegroundColor Yellow
    Write-Host "   1. If app registration is missing: .\recreate-azure-ad-setup.ps1" -ForegroundColor White
    Write-Host "   2. If Key Vault access issues: Check permissions" -ForegroundColor White
    Write-Host "   3. If secrets missing: Re-run secret creation scripts" -ForegroundColor White
    Write-Host "   4. If config files wrong: Manually update with correct values" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "🎉 All tests passed! Your setup is working correctly." -ForegroundColor Green
    Write-Host "   You can now run: npm run dev" -ForegroundColor Cyan
    Write-Host "   Or deploy with: azd up" -ForegroundColor Cyan
}
