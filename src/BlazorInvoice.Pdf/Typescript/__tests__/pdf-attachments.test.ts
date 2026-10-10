import {
    AFRelationship, decodePDFRawStream, PDFArray, PDFDict, PDFDocument, PDFHexString,
    PDFName, PDFNumber, PDFRawStream, PDFString,
} from 'pdf-lib';
import { vi } from 'vitest';
import { PdfA3Converter } from '../pdf-a3-converter';
import { sampleInvoice } from '../pdf-generator';
import { documentId, mockPdfAssets, roundTripXml } from './pdf-test-helpers';

type Encoding = 'plain' | 'flate' | 'filter array' | 'filter chain' | 'unsupported' | 'invalid UTF-8';

async function attachmentPdf(options: {
    name?: string; encoding?: Encoding; location?: 'flat' | 'nested' | 'AF' | 'cyclic';
    hexName?: boolean; unicodeOnly?: boolean;
} = {}): Promise<Uint8Array> {
    const doc = await PDFDocument.create();
    doc.addPage();
    const name = options.name ?? 'invoice.xml';
    const bytes = new TextEncoder().encode(roundTripXml);
    let stream: PDFRawStream;
    if (options.encoding === 'plain') {
        stream = doc.context.stream(bytes);
    } else if (options.encoding === 'invalid UTF-8') {
        stream = doc.context.stream(new Uint8Array([0xff]));
    } else {
        stream = doc.context.flateStream(bytes);
        if (options.encoding === 'filter array') {
            stream.dict.set(PDFName.of('Filter'), doc.context.obj(['FlateDecode']));
        }
        if (options.encoding === 'filter chain') {
            const hex = Array.from(stream.getContents(), byte => byte.toString(16).padStart(2, '0')).join('') + '>';
            stream = doc.context.stream(hex, { Filter: ['ASCIIHexDecode', 'FlateDecode'] });
        }
        if (options.encoding === 'unsupported') stream.dict.set(PDFName.of('Filter'), PDFName.of('DCTDecode'));
    }
    const file = doc.context.obj({
        Type: 'Filespec', F: PDFString.of(name), UF: PDFHexString.fromText(name),
        EF: options.unicodeOnly ? { UF: doc.context.register(stream) } : { F: doc.context.register(stream) },
    });
    const fileRef = doc.context.register(file);
    const tree = doc.context.obj({ Names: [options.hexName ? PDFHexString.fromText(name) : PDFString.of(name), fileRef] });
    if (options.location === 'AF') {
        doc.catalog.set(PDFName.of('AF'), doc.context.obj([fileRef]));
    } else {
        let root = tree;
        if (options.location === 'nested') root = doc.context.obj({ Kids: [doc.context.register(tree)] });
        if (options.location === 'cyclic') {
            root = doc.context.obj({});
            const rootRef = doc.context.register(root);
            root.set(PDFName.of('Kids'), doc.context.obj([rootRef, doc.context.register(tree)]));
        }
        doc.catalog.set(PDFName.of('Names'), doc.context.obj({ EmbeddedFiles: root }));
    }
    return doc.save();
}

afterEach(() => vi.unstubAllGlobals());

describe('invoice XML extraction', () => {
    it.each<Encoding>(['plain', 'flate', 'filter array', 'filter chain'])('decodes %s streams exactly', async encoding => {
        const bytes = await attachmentPdf({ encoding });
        expect(await new PdfA3Converter().getXmlString(bytes)).toBe(roundTripXml);
    });

    it.each(['invoice.xml', 'factur-x.xml', 'zugferd-invoice.xml', 'ZUGFeRD-invoice.xml'])('recognizes %s', async name => {
        expect(await new PdfA3Converter().getXmlString(await attachmentPdf({ name, hexName: true })))
            .toBe(roundTripXml);
    });

    it.each<'nested' | 'AF' | 'cyclic'>(['nested', 'AF', 'cyclic'])('reads %s attachment references', async location => {
        expect(await new PdfA3Converter().getXmlString(await attachmentPdf({ location }))).toBe(roundTripXml);
    });

    it('reads a Unicode filename and EF/UF-only attachment', async () => {
        expect(await new PdfA3Converter().getXmlString(await attachmentPdf({ unicodeOnly: true, location: 'AF' })))
            .toBe(roundTripXml);
    });

    it('prefers invoice.xml over other recognized attachments and ignores unrelated XML', async () => {
        const doc = await PDFDocument.create();
        doc.addPage();
        await doc.attach(new TextEncoder().encode('<Other/>'), 'other.xml');
        await doc.attach(new TextEncoder().encode('<FacturX/>'), 'factur-x.xml');
        await doc.attach(new TextEncoder().encode(roundTripXml), 'invoice.xml');
        expect(await new PdfA3Converter().getXmlString(await doc.save())).toBe(roundTripXml);
        expect(await new PdfA3Converter().getXmlString(await attachmentPdf({ name: 'other.xml' }))).toBeNull();
    });

    it('returns null when no attachment exists', async () => {
        const doc = await PDFDocument.create();
        doc.addPage();
        expect(await new PdfA3Converter().getXmlString(await doc.save())).toBeNull();
    });

    it.each<Encoding>(['unsupported', 'invalid UTF-8'])('rejects %s instead of returning corrupted XML', async encoding => {
        await expect(new PdfA3Converter().getXmlString(await attachmentPdf({ encoding }))).rejects.toThrow();
    });

    it('rejects malformed PDF input', async () => {
        await expect(new PdfA3Converter().getXmlString(new TextEncoder().encode('not a PDF'))).rejects.toThrow();
    });
});

it('embeds invoice XML through the attachment API while preserving existing catalog names and attachments', async () => {
    mockPdfAssets();
    const doc = await PDFDocument.create();
    doc.addPage();
    doc.catalog.set(PDFName.of('Names'), doc.context.obj({ Dests: { Names: [] } }));
    const unrelatedBytes = new TextEncoder().encode('Keep this attachment');
    await doc.attach(unrelatedBytes, 'readme.txt', { afRelationship: AFRelationship.Supplement });
    const bytes = await new PdfA3Converter().createA3Pdf(doc, sampleInvoice(), 'de', documentId, roundTripXml);
    const saved = await PDFDocument.load(bytes, { updateMetadata: false });
    const names = saved.catalog.lookup(PDFName.of('Names'), PDFDict);
    expect(names.has(PDFName.of('Dests'))).toBe(true);
    const tree = names.lookup(PDFName.of('EmbeddedFiles'), PDFDict);
    const entries = tree.lookup(PDFName.of('Names'), PDFArray);
    expect(entries.size()).toBe(4);
    const af = saved.catalog.lookup(PDFName.of('AF'), PDFArray);
    expect(af.size()).toBe(2);
    const existing = entries.lookup(1, PDFDict).lookup(PDFName.of('EF'), PDFDict).lookup(PDFName.of('F'));
    expect(existing).toBeInstanceOf(PDFRawStream);
    expect(decodePDFRawStream(existing as PDFRawStream).decode()).toEqual(unrelatedBytes);
    const invoice = entries.lookup(3, PDFDict);
    expect(invoice.lookup(PDFName.of('AFRelationship'))).toBe(PDFName.of('Alternative'));
    const file = invoice.lookup(PDFName.of('EF'), PDFDict).lookup(PDFName.of('F'));
    expect(file).toBeInstanceOf(PDFRawStream);
    const stream = file as PDFRawStream;
    expect(stream.dict.lookup(PDFName.of('Subtype'))).toBe(PDFName.of('application/xml'));
    expect(stream.dict.lookup(PDFName.of('Params'), PDFDict).lookup(PDFName.of('Size'), PDFNumber).asNumber())
        .toBe(new TextEncoder().encode(roundTripXml).length);
    expect(await new PdfA3Converter().getXmlString(bytes)).toBe(roundTripXml);
});
