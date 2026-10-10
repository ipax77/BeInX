import fs from 'fs';
import path from 'path';
import { DOMParser, onWarningStopParsing } from '@xmldom/xmldom';
import { decodePDFRawStream, PDFArray, PDFDocument, PDFName, PDFRawStream } from 'pdf-lib';
import { vi } from 'vitest';

export const nativeFetch = globalThis.fetch;
export const invoiceXml = fs.readFileSync(path.join(__dirname, 'data/sample.xml'), 'utf8');
export const roundTripXml = '\n  <Invoice><Name>Müller &amp; sons</Name><Note>Łódź 東京 &lt;offer&gt; &quot;yes&quot; &apos;ok&apos;</Note></Invoice>  \n';
export const documentId = '00112233445566778899aabbccddeeff';

export function mockPdfAssets(): void {
    vi.stubGlobal('fetch', vi.fn(async (input: string) => {
        const prefix = './_content/BlazorInvoice.Pdf/';
        const assets = [
            'fonts/Inter-Light.ttf', 'fonts/Inter-Bold.ttf',
            'fonts/Inter-MediumItalic.ttf', 'colorprofiles/sRGB2014.icc',
        ];
        const asset = assets.find(name => input === prefix + name);
        if (!asset) throw new Error(`Unexpected asset request: ${input}`);
        const bytes = fs.readFileSync(path.join(__dirname, '../../wwwroot', asset));
        return new Response(new Uint8Array(bytes).buffer);
    }));
}

export function parseXml(xml: string) {
    return new DOMParser({ onError: onWarningStopParsing }).parseFromString(xml, 'application/xml');
}

export async function extractXmp(pdfBytes: Uint8Array) {
    const pdf = await PDFDocument.load(pdfBytes, { updateMetadata: false });
    const stream = pdf.catalog.lookup(PDFName.of('Metadata'));
    if (!(stream instanceof PDFRawStream)) throw new Error('PDF has no metadata stream');
    const xml = new TextDecoder('utf-8', { fatal: true }).decode(decodePDFRawStream(stream).decode());
    return { pdf, metadata: parseXml(xml) };
}

export async function readPageContent(pdfBytes: Uint8Array): Promise<string[]> {
    const pdf = await PDFDocument.load(pdfBytes, { updateMetadata: false });
    return pdf.getPages().map(page => {
        const contents = page.node.lookup(PDFName.of('Contents'));
        const streams = contents instanceof PDFArray
            ? Array.from({ length: contents.size() }, (_, index) => contents.lookup(index, PDFRawStream))
            : [contents as PDFRawStream];
        return streams.map(stream => new TextDecoder().decode(decodePDFRawStream(stream).decode())).join('\n');
    });
}
