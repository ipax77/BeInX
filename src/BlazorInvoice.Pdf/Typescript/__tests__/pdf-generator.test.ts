import fs from 'fs';
import { PDFDict, PDFDocument, PDFName } from 'pdf-lib';
import { vi } from 'vitest';
import { createInvoicePdfA3Bytes, createInvoicePdfBytes, sampleInvoice } from '../pdf-generator';
import { PdfA3Converter } from '../pdf-a3-converter';
import { documentId, extractXmp, invoiceXml, mockPdfAssets, parseXml, readPageContent, roundTripXml } from './pdf-test-helpers';

beforeEach(mockPdfAssets);
afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

describe('PdfGenerator', () => {
    it('creates a readable ordinary PDF with content on every page', async () => {
        const save = vi.spyOn(PDFDocument.prototype, 'save');
        const bytes = await createInvoicePdfBytes(sampleInvoice(), 'fr');
        expect(save).toHaveBeenCalledTimes(1);
        const contents = await readPageContent(bytes);
        expect(contents.length).toBeGreaterThan(1);
        contents.forEach(content => expect(content).toMatch(/\bTj\b|\bTJ\b/));
        fs.writeFileSync('testinvoice.pdf', bytes);
    });

    it('creates a readable A3 PDF with one save and embedded font mappings', async () => {
        const save = vi.spyOn(PDFDocument.prototype, 'save');
        const bytes = await createInvoicePdfA3Bytes(sampleInvoice(), 'de', documentId, invoiceXml);
        expect(save).toHaveBeenCalledTimes(1);
        const contents = await readPageContent(bytes);
        expect(contents.length).toBeGreaterThan(1);
        contents.forEach(content => expect(content).toMatch(/\bTj\b|\bTJ\b/));
        const pdf = await PDFDocument.load(bytes, { updateMetadata: false });
        let cidFonts = 0;
        for (const [, object] of pdf.context.enumerateIndirectObjects()) {
            if (!(object instanceof PDFDict)) continue;
            if (object.get(PDFName.of('Subtype')) === PDFName.of('CIDFontType2')) {
                cidFonts++;
                expect(object.get(PDFName.of('CIDToGIDMap'))).toBe(PDFName.of('Identity'));
            } else {
                expect(object.has(PDFName.of('CIDToGIDMap'))).toBe(false);
            }
        }
        expect(cidFonts).toBe(3);
        fs.writeFileSync('testinvoiceA3.pdf', bytes);
    });

    it.each([invoiceXml, roundTripXml])('preserves the supplied attachment exactly %#', async xml => {
        const bytes = await createInvoicePdfA3Bytes(sampleInvoice(), 'de', documentId, xml);
        const extracted = await new PdfA3Converter().getXmlString(bytes);
        expect(extracted).toBe(xml);
        if (xml === roundTripXml) {
            const parsed = parseXml(extracted!);
            expect(parsed.getElementsByTagName('Name')[0].textContent).toBe('Müller & sons');
            expect(parsed.getElementsByTagName('Note')[0].textContent).toBe(`Łódź 東京 <offer> "yes" 'ok'`);
        }
    });

    it.each([
        'Müller & sons',
        'Łódź 東京',
        `<offer> "quoted" 'apostrophe' &amp;`,
    ])('round-trips XMP and document information: %s', async value => {
        const invoice = sampleInvoice();
        invoice.sellerParty.name = value;
        invoice.id = `INV-${value}`;
        const bytes = await createInvoicePdfA3Bytes(invoice, 'de', documentId, roundTripXml);
        const { pdf, metadata } = await extractXmp(bytes);
        const dc = 'http://purl.org/dc/elements/1.1/';
        const rdf = 'http://www.w3.org/1999/02/22-rdf-syntax-ns#';
        const xmp = 'http://ns.adobe.com/xap/1.0/';
        const pdfNs = 'http://ns.adobe.com/pdf/1.3/';
        const creator = metadata.getElementsByTagNameNS(dc, 'creator')[0];
        const title = metadata.getElementsByTagNameNS(dc, 'title')[0];
        expect(creator.getElementsByTagNameNS(rdf, 'li')[0].textContent).toBe(value);
        expect(title.getElementsByTagNameNS(rdf, 'li')[0].textContent).toBe(invoice.id);
        expect(metadata.getElementsByTagNameNS(xmp, 'CreatorTool')[0].textContent).toBe(value);
        expect(metadata.getElementsByTagNameNS(pdfNs, 'Producer')[0].textContent).toBe(value);
        expect(pdf.getAuthor()).toBe(value);
        expect(pdf.getCreator()).toBe(value);
        expect(pdf.getProducer()).toBe(value);
        expect(pdf.getTitle()).toBe(invoice.id);
        const issueDate = new Date(invoice.issueDate);
        expect(pdf.getCreationDate()).toEqual(issueDate);
        expect(pdf.getModificationDate()).toEqual(issueDate);
        for (const field of ['CreateDate', 'ModifyDate', 'MetadataDate']) {
            expect(metadata.getElementsByTagNameNS(xmp, field)[0].textContent).toBe(issueDate.toISOString());
        }
    });
});
