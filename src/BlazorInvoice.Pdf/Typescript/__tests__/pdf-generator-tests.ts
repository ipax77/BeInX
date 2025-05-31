import * as fs from 'fs';
import { PdfGenerator, sampleInvoice } from "../pdf-generator";
import { PDFDocument, StandardFonts } from 'pdf-lib';
import { PdfA3Converter } from '../pdf-a3-converter';
import path from 'path';
import fontkit from "@pdf-lib/fontkit";
import { Fontkit } from "pdf-lib/cjs/types/fontkit";
import { randomBytes } from 'crypto';

describe('PdfGenerator', () => {
    it('should create a valid PDF file', async () => {
        const pdfDoc = await PDFDocument.create();
        const defaultFont = await pdfDoc.embedFont(StandardFonts.Helvetica);
        const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
        const italicFont = await pdfDoc.embedFont(StandardFonts.HelveticaOblique);

        const pdfGenerator = new PdfGenerator(pdfDoc, defaultFont, boldFont, italicFont, "fr");

        const invoice = sampleInvoice();
        const pdf = await pdfGenerator.generateInvoice(invoice);
        fs.writeFileSync('testinvoice.pdf', pdf);
    });

    it('should create a valid PDF/A3 file', async () => {
        const pdfDoc = await PDFDocument.create();
        pdfDoc.registerFontkit(fontkit as Fontkit);
        const regularFontBytes = fs.readFileSync(path.join(__dirname, '../../wwwroot/fonts/Inter-Light.ttf'));
        const boldFontBytes = fs.readFileSync(path.join(__dirname, '../../wwwroot/fonts/Inter-Bold.ttf'));
        const italicFontBytes = fs.readFileSync(path.join(__dirname, '../../wwwroot/fonts/Inter-MediumItalic.ttf'));
        const defaultFont = await pdfDoc.embedFont(regularFontBytes, { subset: true });
        const boldFont = await pdfDoc.embedFont(boldFontBytes, { subset: true });
        const italicFont = await pdfDoc.embedFont(italicFontBytes, { subset: true });
        const culture = "de";
        const pdfGenerator = new PdfGenerator(pdfDoc, defaultFont, boldFont, italicFont, culture);

        const invoice = sampleInvoice();
        await pdfGenerator.generateInvoice(invoice);
        
        const xmlInvoice = fs.readFileSync("./__tests__/data/sample.xml",'utf8');
        const pdfA3Converter = new MockPdfA3Converter();
        const documentId = randomBytes(16).toString("hex");
        const pdf = await pdfA3Converter.createA3Pdf(pdfDoc, invoice, culture, documentId, xmlInvoice);

        fs.writeFileSync('testinvoiceA3.pdf', pdf);
    });
  });

  class MockPdfA3Converter extends PdfA3Converter {
    protected override async loadIccProfile(): Promise<Uint8Array> {
        return fs.readFileSync(path.join(__dirname, "../../wwwroot/colorprofiles/sRGB2014.icc"));
    }
}