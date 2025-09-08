# Playwright End-to-End Tests for BlazorInvoice PWA

This project contains **end-to-end tests** for the BlazorInvoice PWA using [Playwright for .NET](https://playwright.dev/dotnet/).

## 🔧 Prerequisites
- .NET 10 SDK (or matching the project)
- [dotnet-serve](https://github.com/natemcmaster/dotnet-serve)  
  Install if you don’t already have it:
  ```powershell
  dotnet tool install --global dotnet-serve
  ```

##  ▶️ How to Run the Tests
### 1. Publish the Blazor PWA

For testing, always publish using the publish profile so that service workers, manifest, and assets are generated correctly. In the folder `src/BlazorInvoice.Pwa` run:
```powershell
dotnet publish -c Release -p:PublishProfile=FolderProfile
```

This will output the published app under:

```
src/BlazorInvoice.Pwa/bin/Release/net10.0/publish
```
### 2. Serve the Published App

Start a local static file server on port 5077 in the published folder `src/BlazorInvoice.Pwa/bin/Release/net10.0/publish/wwwroot`:

```powershell
dotnet-serve -p 5077
```

The app should now be available at
👉 http://localhost:5077

### 3. Run Playwright Tests

From the test project folder `src/BlazorInvoice.Pwa.Tests`, run:
```powershell
dotnet test
```

The tests will navigate to http://localhost:5077, interact with the app, and verify features such as:

* Navigation to the “New Invoice” page
* Creating sample invoices
* Exporting invoices as PDFs
* Checking iframe rendering