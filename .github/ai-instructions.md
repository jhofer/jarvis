# GitHub AI Instruction File for Jarvis SaaS

## Vision

Jarvis will become a SaaS platform offering a suite of productivity tools. The first tool to be implemented is **PDFRenamer**.

## Core Requirements

### Authentication

- Support login via Azure AD for all major identity providers (Microsoft, Google, etc).
- Users must be able to add a connection to their personal Microsoft account to allow background access to OneDrive (required for PDFRenamer functionality).

### Features

- Implement PDFRenamer as the initial tool.
- Provide a user interface for managing OneDrive connections and renaming PDFs.

### Architecture & Stack

- **Frontend:** Next.js (React)
- **Backend:** .NET API
- **Infrastructure:** Aspire for development and deployment
- **Microservices:** Use Dapr for service-to-service communication

## Implementation Notes

- Ensure extensibility for future tools.
- Prioritize secure authentication and authorization flows.
- Design with multi-tenancy in mind for SaaS scalability.
- Refactor to meet clean code standards
- No secrets in code under source control
  - document secrets in notion with the MCP server
  - store secrets in keyvault
        - resource naming pattern: `<resourcetypeprefix>-<name>-<env>-<region>-<tenant>`
            - Note: Azure Key Vault names must follow DNS constraints (3-24 alphanumeric chars, start with a letter, end with letter or digit, no consecutive hyphens).
            - Key Vault for dev (Azure-valid): `kvsecretsdevchtcgjarvis`
- Prefer defining Azure infrastructure with Aspire/azd (`azd provision`) whenever possible. Aspire's provisioning understands the application model and will manage resources for development and CI flows.
- Only use hand-authored Bicep or CLI scripts when Aspire/azd cannot express a needed resource or customization. In that case, keep those templates in `infra/` and document why they are required.

## Querying Microsoft Documentation

You have access to an MCP server called `microsoft.docs.mcp` - this tool allows you to search through Microsoft's latest official documentation, and that information might be more detailed or newer than what's in your training data set.

When handling questions around how to work with native Microsoft technologies, such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, the `dotnet` runtime - please use this tool for research purposes when dealing with specific / narrowly defined questions that may occur.

## Project documentation

- document key conecepts in notion
- document how tos and step by step developer information in the readme files.

---

### Next Steps

- Always `cd` to the project root (`c:\Dev\jarvis`) before running any commands or scripts.
- Scaffold Next.js frontend and .NET backend with Aspire.
- Integrate Azure AD authentication with support for multiple IDPs.
- Implement OneDrive connection and PDFRenamer tool as MVP.
- Set up Dapr for microservice communication.

#### Next Steps to Enable Azure AD Login in the Frontend

1. **Register the Application in Azure AD**
    - Go to the [Azure Portal](https://portal.azure.com/), navigate to Azure Active Directory > App registrations.
    - Register a new application for your frontend (e.g., "Jarvis Frontend").
    - Set the redirect URI to your frontend's URL (e.g., `http://localhost:3000/api/auth/callback/azure-ad`).
    - Note the Application (client) ID and Directory (tenant) ID.
    - Create a client secret and note it securely (do not commit to source control).

2. **Install Required Packages in the Next.js Frontend**
    - Use a library such as `next-auth` for authentication.
    - Run:

      ```pwsh
      cd nextjs-frontend
      npm install next-auth @next-auth/azure-ad
      ```

3. **Configure NextAuth for Azure AD**
    - Create or update `[...nextauth].ts` in `nextjs-frontend/app/api/auth`:
      - Use the Azure AD provider and set clientId, clientSecret, tenantId from environment variables.

4. **Add Environment Variables**
    - In `nextjs-frontend/.env.local`, add:

      ```env
      AZURE_AD_CLIENT_ID=your-client-id
      AZURE_AD_CLIENT_SECRET=your-client-secret
      AZURE_AD_TENANT_ID=your-tenant-id
      NEXTAUTH_URL=http://localhost:3000
      NEXTAUTH_SECRET=your-random-secret
      ```

5. **Update the UI to Add Login/Logout Buttons**
    - Use `useSession` and `signIn`/`signOut` from `next-auth/react` in your components.

6. **Test the Authentication Flow**
    - Start the frontend and verify login with Azure AD works.

7. **(Optional) Add Support for Multiple IDPs**
    - Add additional providers in the NextAuth configuration (e.g., Google, GitHub).

8. **Document Secrets and Configuration**
    - Ensure secrets are not committed to source control.
    - Document secret management in Notion as per project standards.
