import { PDFDocument, PDFFont, StandardFonts } from 'pdf-lib';
import { vi } from 'vitest';
import { createInvoicePdfBytes, PdfGenerator, sampleInvoice } from '../pdf-generator';

afterEach(() => vi.restoreAllMocks());

it.each([
    ['de', '12,34 €', '01.02.25', '01-03.02.25', '01.02.-03.03.'],
    ['fr', '12,34 €', '01/02/25', '01-03/02/25', '01/02-03/03'],
    ['es', '12,34 €', '01/02/25', '01-03/02/25', '1/2-3/3'],
    ['en', '€12.34', '02/01/25', '01-02/03/25', '02/01-03/03'],
])('preserves currency and date formatting for %s', async (locale, currency, sameDay, sameMonth, otherMonth) => {
    const invoice = sampleInvoice();
    invoice.invoiceLines = invoice.invoiceLines.slice(0, 3);
    invoice.invoiceLines.forEach(line => { line.lineTotal = 12.34; line.startDate = '2025-02-01T08:00:00'; });
    invoice.invoiceLines[0].endDate = '2025-02-01T09:00:00';
    invoice.invoiceLines[1].endDate = '2025-02-03T09:00:00';
    invoice.invoiceLines[2].endDate = '2025-03-03T09:00:00';
    const encode = vi.spyOn(PDFFont.prototype, 'encodeText');
    await createInvoicePdfBytes(invoice, locale);
    const text = encode.mock.calls.map(([value]) => value);
    for (const expected of [currency, sameDay, sameMonth, otherMonth, '08:00', '09:00']) {
        expect(text).toContain(expected);
    }
});

it('renders sorted lines without modifying the caller array or objects', async () => {
    const invoice = sampleInvoice();
    const lines = [invoice.invoiceLines[9], invoice.invoiceLines[1], invoice.invoiceLines[0]];
    lines.forEach(line => Object.freeze(line));
    invoice.invoiceLines = Object.freeze(lines) as unknown as typeof invoice.invoiceLines;
    const encode = vi.spyOn(PDFFont.prototype, 'encodeText');
    await createInvoicePdfBytes(invoice, 'de');
    expect(invoice.invoiceLines).toBe(lines);
    expect(invoice.invoiceLines.map(line => line.id)).toEqual(['10', '2', '1']);
    const text = encode.mock.calls.map(([value]) => value);
    expect(text.indexOf(lines[2].name)).toBeLessThan(text.indexOf(lines[1].name));
    expect(text.indexOf(lines[1].name)).toBeLessThan(text.indexOf(lines[0].name));
});

it('reuses formatters when drawing another invoice with the same generator', async () => {
    const doc = await PDFDocument.create();
    const fonts = await Promise.all([
        doc.embedFont(StandardFonts.Helvetica), doc.embedFont(StandardFonts.HelveticaBold),
        doc.embedFont(StandardFonts.HelveticaOblique),
    ]);
    const NumberFormat = Intl.NumberFormat;
    const DateTimeFormat = Intl.DateTimeFormat;
    const numberFormat = vi.spyOn(Intl, 'NumberFormat').mockImplementation(function (...args) {
        return new NumberFormat(...args);
    });
    const dateFormat = vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(function (...args) {
        return new DateTimeFormat(...args);
    });
    const generator = new PdfGenerator(doc, fonts[0], fonts[1], fonts[2], 'de');
    await generator.drawInvoice(sampleInvoice());
    expect(numberFormat).toHaveBeenCalledTimes(1);
    expect(dateFormat).toHaveBeenCalledTimes(3);
    await generator.drawInvoice(sampleInvoice());
    expect(numberFormat).toHaveBeenCalledTimes(1);
    expect(dateFormat).toHaveBeenCalledTimes(3);
});
