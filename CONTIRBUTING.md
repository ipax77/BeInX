# Welcome to BeInX contributing guide

Thank you for investing your time in contributing to our project!

Read our [Code of Conduct](./CODE_OF_CONDUCT.md) to keep our community approachable and respectable.

In this guide you will get an overview of the contribution workflow from opening an issue, creating a PR, reviewing, and merging the PR.

## New contributor guide

To get an overview of the project, read the [README](README.md). Here are some resources to help you get started with open source contributions:

- [Set up Git](https://docs.github.com/en/get-started/quickstart/set-up-git)
- [GitHub flow](https://docs.github.com/en/get-started/quickstart/github-flow)
- [Collaborating with pull requests](https://docs.github.com/en/github/collaborating-with-pull-requests)
- [Contributing](https://gist.github.com/MarcDiethelm/7303312)

## Getting started

### Prerequisits
* .NET SDK (version 9.0 or higher)

### Project Structure
* src with BlazorInvoice.Web as startup project
* src/BlazorInvoice.Pdf/Typescript (Handles PDF generation via TypeScript/Webpack)
* maui (Desktop App with dependencies in src)

### Web Project Setup
1. Navigate to the src/BlazorInvoice.Web directory.
2. Update the database configuration:
3. Modify the DbFile option in appsettings.Development.json to an existing path, the file will be generated during startup (SQLite Db).
4. Run the project using your preferred IDE or the command line:
	`dotnet run`

### PDF Generation Setup
#### Prerequisits
	* Node.js ( recommended: latest LTS)
	* npm
#### Setup
1. Navigate to the BlazorInvoice.Pdf/Typescript folder.
2. Install dependencies: `npm install`
3. Run tests: `npm test`
4. Build the PDF scripts: `npx webpack`

### Localization
Localization strings are maintained in a centralized CSV file:
* Location: `src/BlazorInvoice.Localization/Resources/InvoiceLoc.csv`
* Format: `key;en;de;es;fr`
* Run the `BlazorInvoice.Localization` project to regenerate the `.resx` files based on the updated CSV. 

## Pull requests (PR)

- Please file an issue before you start.
