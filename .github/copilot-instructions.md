# Copilot Instructions for BeInX

## Build, Test, and Lint Commands

### .NET (Blazor PWA)
- **Build:**
  - `dotnet build` (run in `src` directory)
- **Test:**
  - `dotnet test` (run in `src` directory)
  - To run a single test, use: `dotnet test --filter FullyQualifiedName~<TestName>`
- **Restore:**
  - `dotnet restore` (run in `src` directory)

### PDF Generation (TypeScript)
- **Setup:**
  - Navigate to `src/BlazorInvoice.Pdf/Typescript`
  - `npm install`
- **Build:**
  - `npm run build` (uses webpack)
- **Test:**
  - `npm test` (runs Vitest)
  - To run a single test: `npx vitest run __tests__/pdf-generator.test.ts -t <test name>`
- **Lint:**
  - If ESLint is configured: `npx eslint .`

## High-Level Architecture
- **Blazor WebAssembly PWA**: Main app in `src/beinx.pwa` (entry: `Program.cs`).
- **PDF Generation**: Handled by TypeScript in `src/BlazorInvoice.Pdf/Typescript`, built with webpack.
- **Localization**: Centralized in `src/beinx.locbuilder/Resources/InvoiceLoc.csv`. Run the `beinx.locbuilder` project to regenerate `.resx` files after updates.
- **Data Storage**: Uses IndexedDB in browser for offline support.
- **Multi-language**: English, German, Spanish, French supported.
- **Project References**: `beinx.pwa` references `beinx.db`, `beinx.web`, `beinx.shared`, and `BlazorInvoice.Pdf`.

## Key Conventions
- **Localization**: All translation keys and values are managed in a single CSV. Update this file and regenerate resources as needed.
- **PDF/TypeScript**: All PDF logic and tests are in `src/BlazorInvoice.Pdf/Typescript`. Use Vitest for testing.
- **.NET Version**: Project targets .NET 10.0 or higher.
- **PWA**: App is installable and works offline after first load.
- **Legal**: See `DISCLAIMER.md` and `LICENSE.md` for usage terms.

---

This file summarizes build/test commands, architecture, and conventions for Copilot and future contributors. Would you like to adjust anything or add coverage for other areas?