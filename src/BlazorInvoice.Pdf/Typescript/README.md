# PDF generation tests

Run from this directory:

```powershell
npm ci
npm test -- --run
npm run build
```

The tests check saved PDF content, font mappings, exact invoice XML attachment
round-trips, XMP text and document information, and ICC profile caching/retries.
XML parsing checks well-formedness and recovered text; invoice schema and business
rule validation belong to the caller.

PDF/A-3b integration tests are skipped unless `VERAPDF_URL` is set. With the local
veraPDF REST container running:

```powershell
$env:VERAPDF_URL = 'http://localhost:8080'
npm test -- --run
Remove-Item Env:VERAPDF_URL
```

Configured integration tests fail on service errors, invalid reports, or
non-compliant PDFs. Each request has a 30-second timeout.

The generation tests refresh `testinvoice.pdf` and `testinvoiceA3.pdf`. The webpack
build refreshes the browser bundle in `../wwwroot/dist/pdf-generator.js`.
