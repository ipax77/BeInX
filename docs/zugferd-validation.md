# ZUGFeRD XML export validation

PDF/A-3 export validates the exact generated CII XML before invoking PDF interop
when `ExportValidate` is enabled. Failures include official rule IDs, messages,
and XML locations, and prevent PDF generation and invoice finalization.
`SchematronValidationUri` remains a setting for the existing UBL validation path;
ZUGFeRD validation is local. XML previews and imports do not trigger validation.

The mapper retains ZUGFeRD-csharp 18.0.0 with `Version23`, Comfort, and CII.
It preserves the required empty header delivery container suppressed by that
writer and uses BeInX's displayed rounded line amounts consistently in the XML
line and header totals. No optional delivery date is invented.

Validation comes from `pax.XRechnung.NET` 0.3.3, using the untouched official FeRD
ZUGFeRD 2.3.3 / Factur-X 1.07.3 EN16931 schemas, compiled XSLT, and code lists.
Source ZIP paths, hashes, and Apache-2.0 attribution are in the adjacent library
repository's `src/pax.XRechnung.NET/Resources/Zugferd/2.3.3/PROVENANCE.md`.

## Local development package

The new 0.3.3 package has been built locally; this task does not publish it to
NuGet. Before restoring BeInX on a fresh machine, build it in the adjacent repo:

```powershell
dotnet pack ../pax.XRechnung.NET/src/pax.XRechnung.NET -c Release -o ../pax.XRechnung.NET/artifacts/official-feed
```

Use a NuGet.config with both that absolute local feed path and nuget.org, then
run `dotnet restore --configfile <config-path>` and `dotnet test tests/beinx.Tests`.
Do not reuse a cached development 0.3.3 package from before the official-artifact
replacement. The restored production assembly must pass the library's embedded
resource SHA-256 tests.

XML validation does not certify the PDF/A carrier. Cold stylesheet compilation
runs on the calling thread; measured offline browser timings are recorded in
the library's `tools/Validator.WasmProbe/README.md`.
