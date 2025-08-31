#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick Azure AD app registration recreation script.

.DESCRIPTION
    Simplified version that just recreates the app registration and updates Key Vault.
    Use this when you just need to quickly restore a deleted app registration.

.EXAMPLE
    .\quick-recreate-app.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "🚀 Quick Azure AD App Recreation" -ForegroundColor Green

# Configuration
$AppName = "Jarvis-NextJS-Auth"
$KeyVaultName = "secrets-3kktkazybj2b2"
$LocalRedirectUri = "http://localhost:3000/api/auth/callback/azure-ad"
$ProdRedirectUri = "https://nextjs-frontend.whitepebble-d22a3c98.westeurope.azurecontainerapps.io/api/auth/callback/azure-ad"

try {
    # Check login
    $account = az account show --query "user.name" -o tsv
    Write-Host "✅ Logged in as: $account" -ForegroundColor Green
    
    # Get tenant ID
    $tenantId = az account show --query "tenantId" -o tsv
    
    # Create app registration
    Write-Host "📝 Creating app registration..." -ForegroundColor Yellow
    $app = az ad app create --display-name $AppName --query "{appId:appId}" -o json | ConvertFrom-Json
    $clientId = $app.appId
    
    # Configure redirect URIs
    Write-Host "🔗 Configuring redirect URIs..." -ForegroundColor Yellow
    az ad app update --id $clientId --web-redirect-uris $LocalRedirectUri $ProdRedirectUri
    
    # Create client secret
    Write-Host "🔐 Creating client secret..." -ForegroundColor Yellow
    $secret = az ad app credential reset --id $clientId --display-name "JarvisSecret" --years 2 --query "password" -o tsv
    
    # Generate NextAuth secret
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $nextAuthSecret = [System.Convert]::ToBase64String($bytes).Substring(0, 32)
    
    # Update Key Vault
    Write-Host "🔑 Updating Key Vault..." -ForegroundColor Yellow
    az keyvault secret set --vault-name $KeyVaultName --name "AZURE-AD-CLIENT-SECRET" --value $secret | Out-Null
    az keyvault secret set --vault-name $KeyVaultName --name "NEXTAUTH-SECRET" --value $nextAuthSecret | Out-Null
    
    # Summary
    Write-Host ""
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Client ID: $clientId" -ForegroundColor Cyan
    Write-Host "Tenant ID: $tenantId" -ForegroundColor Cyan
    Write-Host "Secrets updated in Key Vault: $KeyVaultName" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🔧 Next steps:" -ForegroundColor Yellow
    Write-Host "1. Update .env.local with new Client ID: $clientId" -ForegroundColor White
    Write-Host "2. Update Program.cs with new Client ID" -ForegroundColor White
    Write-Host "3. Run: npm run load-secrets (if you have the script)" -ForegroundColor White
    Write-Host "4. Test with: npm run dev" -ForegroundColor White
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
