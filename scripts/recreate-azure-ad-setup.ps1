#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Recreates the Azure AD app registration and all related components for the Jarvis Next.js application.

.DESCRIPTION
    This script will:
    1. Create a new Azure AD app registration
    2. Configure redirect URIs for local and production environments
    3. Generate a new client secret
    4. Update the Key Vault with the new secrets
    5. Update configuration files with the new app registration details
    6. Provide summary of what was created

.PARAMETER AppName
    Name for the Azure AD app registration (default: "Jarvis-NextJS-Auth")

.PARAMETER KeyVaultName
    Name of the Azure Key Vault to store secrets (default: "secrets-3kktkazybj2b2")

.PARAMETER ProductionUrl
    Production URL for redirect URIs (default: current production URL)

.EXAMPLE
    .\recreate-azure-ad-setup.ps1
    
.EXAMPLE
    .\recreate-azure-ad-setup.ps1 -AppName "MyApp-Auth" -KeyVaultName "my-keyvault"
#>

param(
    [string]$AppName = "Jarvis-NextJS-Auth",
    [string]$KeyVaultName = "secrets-3kktkazybj2b2",
    [string]$ProductionUrl = "https://nextjs-frontend.whitepebble-d22a3c98.westeurope.azurecontainerapps.io"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$NextJSRoot = Join-Path $ProjectRoot "nextjs-frontend"
$AspireHostRoot = Join-Path $ProjectRoot "JarvisAspireHost"

Write-Host "🚀 Starting Azure AD App Registration Recreation Process" -ForegroundColor Green
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Cyan
Write-Host "App Name: $AppName" -ForegroundColor Cyan
Write-Host "Key Vault: $KeyVaultName" -ForegroundColor Cyan
Write-Host "Production URL: $ProductionUrl" -ForegroundColor Cyan
Write-Host ""

# Function to check if Azure CLI is logged in
function Test-AzureLogin {
    try {
        $account = az account show 2>$null | ConvertFrom-Json
        if ($account) {
            Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host "❌ Not logged in to Azure CLI" -ForegroundColor Red
        return $false
    }
    return $false
}

# Function to get current tenant ID
function Get-TenantId {
    try {
        $account = az account show --query "tenantId" -o tsv
        return $account.Trim()
    }
    catch {
        throw "Failed to get tenant ID"
    }
}

# Function to create Azure AD app registration
function New-AzureADAppRegistration {
    param(
        [string]$DisplayName,
        [string]$LocalRedirectUri = "http://localhost:3000/api/auth/callback/azure-ad",
        [string]$ProdRedirectUri
    )
    
    Write-Host "📝 Creating Azure AD App Registration: $DisplayName" -ForegroundColor Yellow
    
    # Create the app registration
    $app = az ad app create --display-name $DisplayName --query "{appId:appId,objectId:id}" -o json | ConvertFrom-Json
    
    if (-not $app.appId) {
        throw "Failed to create app registration"
    }
    
    Write-Host "✅ Created app registration with ID: $($app.appId)" -ForegroundColor Green
    
    # Configure redirect URIs
    $redirectUris = @($LocalRedirectUri)
    if ($ProdRedirectUri) {
        $redirectUris += "$ProdRedirectUri/api/auth/callback/azure-ad"
    }
    
    Write-Host "🔗 Configuring redirect URIs..." -ForegroundColor Yellow
    $redirectUriList = $redirectUris -join ' '
    az ad app update --id $app.appId --web-redirect-uris $redirectUris
    
    Write-Host "✅ Configured redirect URIs:" -ForegroundColor Green
    foreach ($uri in $redirectUris) {
        Write-Host "   - $uri" -ForegroundColor Gray
    }
    
    return $app
}

# Function to create client secret
function New-ClientSecret {
    param(
        [string]$AppId,
        [string]$SecretDisplayName = "JarvisSecret",
        [int]$Years = 2
    )
    
    Write-Host "🔐 Creating client secret..." -ForegroundColor Yellow
    
    $secret = az ad app credential reset --id $AppId --display-name $SecretDisplayName --years $Years --output json | ConvertFrom-Json
    
    if (-not $secret.password) {
        throw "Failed to create client secret"
    }
    
    Write-Host "✅ Created client secret (expires: $($secret.endDateTime))" -ForegroundColor Green
    
    return $secret.password
}

# Function to generate NextAuth secret
function New-NextAuthSecret {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $secret = [System.Convert]::ToBase64String($bytes).Substring(0, 32)
    return $secret
}

# Function to update Key Vault secrets
function Set-KeyVaultSecrets {
    param(
        [string]$VaultName,
        [string]$ClientSecret,
        [string]$NextAuthSecret
    )
    
    Write-Host "🔑 Updating Key Vault secrets..." -ForegroundColor Yellow
    
    # Check if we have access to the Key Vault
    try {
        az keyvault secret list --vault-name $VaultName --output table | Out-Null
    }
    catch {
        Write-Host "❌ No access to Key Vault: $VaultName" -ForegroundColor Red
        Write-Host "Attempting to grant permissions..." -ForegroundColor Yellow
        
        $userEmail = az account show --query "user.name" -o tsv
        az role assignment create --assignee $userEmail --role "Key Vault Secrets Officer" --scope "/subscriptions/$(az account show --query 'id' -o tsv)/resourceGroups/rg-dev/providers/Microsoft.KeyVault/vaults/$VaultName"
        
        Write-Host "⏳ Waiting for permission propagation (30 seconds)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
    }
    
    # Set the secrets
    Write-Host "Setting AZURE-AD-CLIENT-SECRET..." -ForegroundColor Gray
    az keyvault secret set --vault-name $VaultName --name "AZURE-AD-CLIENT-SECRET" --value $ClientSecret | Out-Null
    
    Write-Host "Setting NEXTAUTH-SECRET..." -ForegroundColor Gray
    az keyvault secret set --vault-name $VaultName --name "NEXTAUTH-SECRET" --value $NextAuthSecret | Out-Null
    
    Write-Host "✅ Key Vault secrets updated successfully" -ForegroundColor Green
}

# Function to update configuration files
function Update-ConfigurationFiles {
    param(
        [string]$ClientId,
        [string]$TenantId,
        [string]$ClientSecret,
        [string]$NextAuthSecret
    )
    
    Write-Host "📝 Updating configuration files..." -ForegroundColor Yellow
    
    # Update .env.local.secrets for local development
    $envSecretsPath = Join-Path $NextJSRoot ".env.local.secrets"
    $envSecretsContent = @"
# LOCAL DEVELOPMENT SECRETS - NOT IN SOURCE CONTROL
# Copy these values for local development only

# Azure AD App Registration: $ClientId
AZURE_AD_CLIENT_SECRET=$ClientSecret

# NextAuth Secret
NEXTAUTH_SECRET=$NextAuthSecret

# Instructions to get secrets from Key Vault:
# az keyvault secret show --vault-name "$KeyVaultName" --name "AZURE-AD-CLIENT-SECRET" --query "value" -o tsv
# az keyvault secret show --vault-name "$KeyVaultName" --name "NEXTAUTH-SECRET" --query "value" -o tsv
"@
    
    $envSecretsContent | Out-File -FilePath $envSecretsPath -Encoding UTF8 -Force
    Write-Host "✅ Updated: $envSecretsPath" -ForegroundColor Green
    
    # Update .env.local with public values
    $envLocalPath = Join-Path $NextJSRoot ".env.local"
    $envLocalContent = @"
# Azure AD Configuration - PUBLIC VALUES ONLY
AZURE_AD_CLIENT_ID=$ClientId
AZURE_AD_TENANT_ID=$TenantId

# NextAuth Configuration - LOCAL DEVELOPMENT ONLY
NEXTAUTH_URL=http://localhost:3000

# NOTE: Secrets are stored in Azure Key Vault for production
# For local development, you need to set these secrets locally:
# AZURE_AD_CLIENT_SECRET=<get from Key Vault or Azure portal>
# NEXTAUTH_SECRET=<get from Key Vault or generate locally>

# For production, update NEXTAUTH_URL to your deployed URL
# NEXTAUTH_URL=$ProductionUrl
"@
    
    $envLocalContent | Out-File -FilePath $envLocalPath -Encoding UTF8 -Force
    Write-Host "✅ Updated: $envLocalPath" -ForegroundColor Green
    
    # Update Program.cs in Aspire Host
    $programCsPath = Join-Path $AspireHostRoot "Program.cs"
    if (Test-Path $programCsPath) {
        $programContent = Get-Content $programCsPath -Raw
        
        # Update the client ID in the WithEnvironment call
        $programContent = $programContent -replace 'WithEnvironment\("AZURE_AD_CLIENT_ID", "[^"]*"\)', "WithEnvironment(`"AZURE_AD_CLIENT_ID`", `"$ClientId`")"
        $programContent = $programContent -replace 'WithEnvironment\("AZURE_AD_TENANT_ID", "[^"]*"\)', "WithEnvironment(`"AZURE_AD_TENANT_ID`", `"$TenantId`")"
        
        $programContent | Out-File -FilePath $programCsPath -Encoding UTF8 -Force
        Write-Host "✅ Updated: $programCsPath" -ForegroundColor Green
    }
}

# Function to create deployment notes
function New-DeploymentNotes {
    param(
        [string]$ClientId,
        [string]$TenantId,
        [string]$ObjectId
    )
    
    $notesPath = Join-Path $ProjectRoot "azure-ad-setup-notes.md"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    $notes = @"
# Azure AD App Registration Setup Notes

**Created:** $timestamp  
**Script:** recreate-azure-ad-setup.ps1

## App Registration Details

- **Application Name:** $AppName
- **Application (Client) ID:** $ClientId
- **Object ID:** $ObjectId
- **Tenant ID:** $TenantId

## Configured Redirect URIs

- `http://localhost:3000/api/auth/callback/azure-ad` (Development)
- `$ProductionUrl/api/auth/callback/azure-ad` (Production)

## Key Vault Configuration

- **Key Vault Name:** $KeyVaultName
- **Secrets Stored:**
  - `AZURE-AD-CLIENT-SECRET` - Azure AD application client secret
  - `NEXTAUTH-SECRET` - NextAuth.js encryption secret

## Files Updated

- `nextjs-frontend/.env.local` - Public configuration values
- `nextjs-frontend/.env.local.secrets` - Local development secrets (not in source control)
- `JarvisAspireHost/Program.cs` - Production configuration

## Recovery Commands

If you need to retrieve the secrets from Key Vault:

```powershell
# Get client secret
az keyvault secret show --vault-name "$KeyVaultName" --name "AZURE-AD-CLIENT-SECRET" --query "value" -o tsv

# Get NextAuth secret
az keyvault secret show --vault-name "$KeyVaultName" --name "NEXTAUTH-SECRET" --query "value" -o tsv
```

## Testing

1. **Local Development:**
   ```bash
   cd nextjs-frontend
   npm run dev
   ```

2. **Production Deployment:**
   ```bash
   cd JarvisAspireHost
   azd up
   ```

## Azure Portal Links

- [App Registration](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/$ClientId)
- [Key Vault](https://portal.azure.com/#@$TenantId/resource/subscriptions/$(az account show --query 'id' -o tsv)/resourceGroups/rg-dev/providers/Microsoft.KeyVault/vaults/$KeyVaultName/overview)

---
*This file was auto-generated. Keep it for reference but don't commit secrets to source control.*
"@
    
    $notes | Out-File -FilePath $notesPath -Encoding UTF8 -Force
    Write-Host "✅ Created deployment notes: $notesPath" -ForegroundColor Green
}

# Main execution
try {
    # Check prerequisites
    if (-not (Test-AzureLogin)) {
        Write-Host "Please log in to Azure CLI first:" -ForegroundColor Red
        Write-Host "az login" -ForegroundColor Yellow
        exit 1
    }
    
    # Get tenant ID
    $tenantId = Get-TenantId
    Write-Host "Using Tenant ID: $tenantId" -ForegroundColor Cyan
    
    # Create app registration
    $prodRedirectBase = $ProductionUrl
    $app = New-AzureADAppRegistration -DisplayName $AppName -ProdRedirectUri $prodRedirectBase
    
    # Create client secret
    $clientSecret = New-ClientSecret -AppId $app.appId
    
    # Generate NextAuth secret
    $nextAuthSecret = New-NextAuthSecret
    Write-Host "✅ Generated NextAuth secret" -ForegroundColor Green
    
    # Update Key Vault
    Set-KeyVaultSecrets -VaultName $KeyVaultName -ClientSecret $clientSecret -NextAuthSecret $nextAuthSecret
    
    # Update configuration files
    Update-ConfigurationFiles -ClientId $app.appId -TenantId $tenantId -ClientSecret $clientSecret -NextAuthSecret $nextAuthSecret
    
    # Create deployment notes
    New-DeploymentNotes -ClientId $app.appId -TenantId $tenantId -ObjectId $app.objectId
    
    # Final summary
    Write-Host ""
    Write-Host "🎉 Azure AD App Registration Recreation Complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Summary:" -ForegroundColor Cyan
    Write-Host "   App Name: $AppName" -ForegroundColor White
    Write-Host "   Client ID: $($app.appId)" -ForegroundColor White
    Write-Host "   Tenant ID: $tenantId" -ForegroundColor White
    Write-Host "   Key Vault: $KeyVaultName" -ForegroundColor White
    Write-Host ""
    Write-Host "📁 Files Updated:" -ForegroundColor Cyan
    Write-Host "   - nextjs-frontend/.env.local" -ForegroundColor White
    Write-Host "   - nextjs-frontend/.env.local.secrets" -ForegroundColor White
    Write-Host "   - JarvisAspireHost/Program.cs" -ForegroundColor White
    Write-Host ""
    Write-Host "🔐 Secrets stored in Key Vault:" -ForegroundColor Cyan
    Write-Host "   - AZURE-AD-CLIENT-SECRET" -ForegroundColor White
    Write-Host "   - NEXTAUTH-SECRET" -ForegroundColor White
    Write-Host ""
    Write-Host "✅ You can now test locally with: npm run dev" -ForegroundColor Green
    Write-Host "✅ Deploy to production with: azd up" -ForegroundColor Green
    Write-Host ""
    Write-Host "📖 See azure-ad-setup-notes.md for detailed information" -ForegroundColor Yellow
    
}
catch {
    Write-Host ""
    Write-Host "❌ Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔍 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Ensure you're logged in: az login" -ForegroundColor White
    Write-Host "   2. Check permissions to create app registrations" -ForegroundColor White
    Write-Host "   3. Verify Key Vault access permissions" -ForegroundColor White
    Write-Host "   4. Run script with -Verbose for more details" -ForegroundColor White
    exit 1
}
