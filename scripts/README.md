# Azure AD App Registration Recovery Scripts

This folder contains PowerShell scripts to recreate and validate the Azure AD app registration setup for the Jarvis Next.js application.

## Scripts Overview

### 🔄 `recreate-azure-ad-setup.ps1`
**Full recreation script** - Completely recreates the Azure AD app registration and all related configuration.

**What it does:**
- Creates new Azure AD app registration
- Configures redirect URIs for local and production
- Generates new client secret (2-year expiry)
- Generates new NextAuth secret
- Updates Key Vault with secrets
- Updates all configuration files
- Creates deployment notes

**Usage:**
```powershell
# Basic usage
.\recreate-azure-ad-setup.ps1

# Custom parameters
.\recreate-azure-ad-setup.ps1 -AppName "MyApp-Auth" -KeyVaultName "my-vault"
```

### ⚡ `quick-recreate-app.ps1`
**Quick recreation script** - Fast recreation when you just need a new app registration.

**What it does:**
- Creates new Azure AD app registration
- Sets up redirect URIs
- Creates client secret
- Updates Key Vault secrets
- Provides summary with next steps

**Usage:**
```powershell
.\quick-recreate-app.ps1
```

### 🔍 `validate-setup.ps1`
**Validation script** - Checks if everything is properly configured.

**What it checks:**
- Azure CLI login status
- App registration exists and is accessible
- Redirect URIs are configured correctly
- Key Vault access and secrets
- Configuration files have correct values
- Dependencies are installed

**Usage:**
```powershell
.\validate-setup.ps1
```

## Prerequisites

1. **Azure CLI** installed and logged in:
   ```powershell
   az login
   ```

2. **PowerShell 5.1+** or **PowerShell Core 7+**

3. **Permissions:**
   - Application Developer role in Azure AD (to create app registrations)
   - Key Vault Secrets Officer role on the Key Vault

## Common Recovery Scenarios

### Scenario 1: App Registration Deleted
```powershell
# Run full recreation
.\recreate-azure-ad-setup.ps1

# Validate everything is working
.\validate-setup.ps1

# Test locally
cd ..\nextjs-frontend
npm run dev
```

### Scenario 2: Lost Client Secret
```powershell
# Quick recreation (creates new secret)
.\quick-recreate-app.ps1

# Update local secrets file
cd ..\nextjs-frontend
.\load-secrets.ps1
```

### Scenario 3: Key Vault Access Issues
```powershell
# Grant yourself permissions
$userEmail = az account show --query "user.name" -o tsv
$subscriptionId = az account show --query "id" -o tsv
az role assignment create --assignee $userEmail --role "Key Vault Secrets Officer" --scope "/subscriptions/$subscriptionId/resourceGroups/rg-dev/providers/Microsoft.KeyVault/vaults/secrets-3kktkazybj2b2"

# Wait for propagation and validate
Start-Sleep 30
.\validate-setup.ps1
```

### Scenario 4: Configuration Files Corrupted
```powershell
# Run full recreation to restore all files
.\recreate-azure-ad-setup.ps1
```

## What Gets Created/Updated

### Azure Resources
- **App Registration** in Azure AD
- **Client Secret** (2-year expiry)
- **Redirect URIs** for local and production
- **Key Vault Secrets:**
  - `AZURE-AD-CLIENT-SECRET`
  - `NEXTAUTH-SECRET`

### Local Files
- `nextjs-frontend/.env.local` - Public configuration
- `nextjs-frontend/.env.local.secrets` - Local secrets (not in git)
- `JarvisAspireHost/Program.cs` - Production configuration
- `azure-ad-setup-notes.md` - Deployment notes

## Security Notes

- ✅ **Secrets are stored in Azure Key Vault**
- ✅ **Local secrets file is in .gitignore**
- ✅ **No secrets in source control**
- ✅ **Client secrets have 2-year expiry**
- ⚠️ **Delete azure-ad-setup-notes.md before committing** (contains IDs)

## Troubleshooting

### Permission Errors
```powershell
# Check current user
az account show

# Grant app registration permissions
# (Contact Azure AD admin if you don't have permissions)

# Grant Key Vault permissions
az role assignment create --assignee "your-email@domain.com" --role "Key Vault Secrets Officer" --scope "/subscriptions/SUB-ID/resourceGroups/rg-dev/providers/Microsoft.KeyVault/vaults/VAULT-NAME"
```

### Script Execution Policy
```powershell
# If scripts won't run
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Key Vault Not Found
```powershell
# List available Key Vaults
az keyvault list --output table

# Update scripts with correct vault name
```

## File Structure After Running Scripts

```
jarvis/
├── scripts/
│   ├── recreate-azure-ad-setup.ps1
│   ├── quick-recreate-app.ps1
│   ├── validate-setup.ps1
│   └── README.md (this file)
├── nextjs-frontend/
│   ├── .env.local (public config)
│   ├── .env.local.secrets (local secrets)
│   └── load-secrets.ps1
├── JarvisAspireHost/
│   └── Program.cs (updated)
└── azure-ad-setup-notes.md (generated)
```

## Quick Reference

| Task | Script | Time |
|------|--------|------|
| Full recreation | `.\recreate-azure-ad-setup.ps1` | 2-3 min |
| Quick app creation | `.\quick-recreate-app.ps1` | 30 sec |
| Check everything | `.\validate-setup.ps1` | 10 sec |

---

**Need help?** Run `.\validate-setup.ps1` first to see what's broken, then use the appropriate recreation script.
