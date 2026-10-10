# PDF generation tests

Run from this directory:

```powershell
npm ci
npm test -- --run
npm run build
```

The tests check saved PDF content, font mappings, exact invoice XML attachment
round-trips, XMP text and document information, font-aware text/measurement caching,
and ICC/font source caching and retries.
XML parsing checks well-formedness and recovered text; invoice schema and business
rule validation belong to the caller.

The Inter font files are loaded once per module session, sharing in-flight
requests across invoices. Failed loads are retried without reloading successful
files. Each PDF still embeds its own fonts and subsets. Text and measurement
caches use font object identity and Unicode code points; unsupported characters
continue to be replaced with `?`.

Generated PDFs attach the supplied UTF-8 XML as `invoice.xml` with an
`Alternative` relationship. Import extraction recognizes `invoice.xml`,
`factur-x.xml`, and `zugferd-invoice.xml` case-insensitively, preferring
`invoice.xml` if several recognized attachments exist. It reads flat or nested
attachment name trees and associated files (`/AF`), and decodes the stream's PDF
filters, including uncompressed streams. Unrelated XML attachments are ignored.
No matching attachment returns `null`; PDF parsing errors, unsupported stream
filters, and invalid UTF-8 propagate to the caller.

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
