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

## Querying Microsoft Documentation

You have access to an MCP server called `microsoft.docs.mcp` - this tool allows you to search through Microsoft's latest official documentation, and that information might be more detailed or newer than what's in your training data set.

When handling questions around how to work with native Microsoft technologies, such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, the `dotnet` runtime - please use this tool for research purposes when dealing with specific / narrowly defined questions that may occur.


---

### Next Steps

- Always `cd` to the project root (`c:\Dev\jarvis`) before running any commands or scripts.
- Scaffold Next.js frontend and .NET backend with Aspire.
- Integrate Azure AD authentication with support for multiple IDPs.
- Implement OneDrive connection and PDFRenamer tool as MVP.
- Set up Dapr for microservice communication.
