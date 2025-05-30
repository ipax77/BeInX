import * as fs from 'fs';
import { PdfGenerator, sampleInvoice } from "../pdf-generator";
import { PDFDocument, StandardFonts } from 'pdf-lib';
import { PdfA3Converter } from '../pdf-a3-converter';
import path from 'path';

describe('PdfGenerator', () => {
    it('should create a valid PDF file', async () => {
        const pdfDoc = await PDFDocument.create();
        const defaultFont = await pdfDoc.embedFont(StandardFonts.Helvetica);
        const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
        const italicFont = await pdfDoc.embedFont(StandardFonts.HelveticaOblique);

        const pdfGenerator = new PdfGenerator(pdfDoc, defaultFont, boldFont, italicFont, "fr");

        const invoice = sampleInvoice();
        const logo = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
        // invoice.sellerParty.logo = { base64String: logo };
        const pdf = await pdfGenerator.generateInvoice(invoice);
        fs.writeFileSync('testinvoice.pdf', pdf);
    });

    it('should create a valid PDF/A3 file', async () => {
        const pdfDoc = await PDFDocument.create();
        const defaultFont = await pdfDoc.embedFont(StandardFonts.Helvetica);
        const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
        const italicFont = await pdfDoc.embedFont(StandardFonts.HelveticaOblique);

        const pdfGenerator = new PdfGenerator(pdfDoc, defaultFont, boldFont, italicFont, "fr");

        const invoice = sampleInvoice();
        const logo = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
        // invoice.sellerParty.logo = { base64String: logo };
        await pdfGenerator.generateInvoice(invoice);
        
        const pdfA3Converter = new MockPdfA3Converter();
        const pdf = await pdfA3Converter.createA3Pdf(pdfDoc, invoice, "de", "<invoice>Test</invoice>");

        fs.writeFileSync('testinvoiceA3.pdf', pdf);
    });
  });

  class MockPdfA3Converter extends PdfA3Converter {
    protected override async loadIccProfile(): Promise<Uint8Array> {
        return fs.readFileSync(path.join(__dirname, "../../wwwroot/colorprofiles/sRGB2014.icc"));
    }
}