import * as fs from 'fs';
import { PdfGenerator, sampleInvoice } from "../pdf-generator";
import { PDFDocument, StandardFonts } from 'pdf-lib';

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
  });