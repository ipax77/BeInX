import { vi } from 'vitest';
import { createInvoicePdfA3Bytes, sampleInvoice } from '../pdf-generator';
import { documentId, invoiceXml, mockPdfAssets, nativeFetch, roundTripXml } from './pdf-test-helpers';

const serviceUrl = process.env.VERAPDF_URL;

describe.skipIf(!serviceUrl)('veraPDF integration', () => {
    beforeEach(mockPdfAssets);
    afterEach(() => vi.unstubAllGlobals());

    it.each(['sample', 'special characters', 'Unicode'])('validates PDF/A-3b: %s', async scenario => {
        const invoice = sampleInvoice();
        if (scenario === 'special characters') {
            invoice.sellerParty.name = `Müller & sons <offer> "quoted" 'apostrophe'`;
            invoice.id = 'INV-<1>&2';
        }
        if (scenario === 'Unicode') {
            invoice.sellerParty.name = 'Müller Łódź 東京';
            invoice.id = 'INV-Łódź-東京';
        }
        const bytes = await createInvoicePdfA3Bytes(invoice, 'de', documentId,
            scenario === 'sample' ? invoiceXml : roundTripXml);
        const body = new FormData();
        body.append('file', new Blob([new Uint8Array(bytes).buffer], { type: 'application/pdf' }), 'invoice.pdf');
        const response = await nativeFetch(`${serviceUrl!.replace(/\/$/, '')}/api/validate/3b`, {
            method: 'POST', headers: { Accept: 'application/json' }, body,
            signal: AbortSignal.timeout(30_000),
        });
        expect(response.ok, `veraPDF HTTP ${response.status}`).toBe(true);
        const report = await response.json();
        const jobs = report.report.jobs;
        expect(jobs).toHaveLength(1);
        expect(jobs[0].validationResult).toHaveLength(1);
        const result = jobs[0].validationResult[0];
        expect(result.jobEndStatus).toBe('normal');
        expect(result.profileName).toBe('PDF/A-3b validation profile');
        expect(result.compliant, JSON.stringify(result.details)).toBe(true);
        expect(result.details.failedRules).toBe(0);
    }, 45_000);
});
