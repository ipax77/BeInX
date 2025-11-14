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
* .NET SDK (version 10.0 or higher)

### Project Structure
* src with beinx.pwa as startup project
* src/BlazorInvoice.Pdf/Typescript (Handles PDF generation via TypeScript/Webpack)

### IndexedDb
#### Prerequisits
	* Node.js ( recommended: latest LTS)
	* npm
#### Setup
1. Navigate to the beinx.db/Client folder.
2. Install dependencies: `npm install`
3. Run tests: `npm test`
4. Build the db scripts: `npx webpack`

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
* Location: `src/beinx.locbuilder/Resources/InvoiceLoc.csv`
* Format: `key;en;de;es;fr`
* Run the `beinx.locbuilder` project to regenerate the `.resx` files based on the updated CSV. 

## Pull requests (PR)

- Please file an issue before you start.
