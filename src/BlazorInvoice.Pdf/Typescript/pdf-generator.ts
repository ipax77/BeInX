import {
  PDFDocument,
  PDFFont,
  PDFImage,
  PDFPage,
  PageSizes,
  StandardFonts,
} from "pdf-lib";
import fontkit from "@pdf-lib/fontkit";
import { Fontkit } from "pdf-lib/cjs/types/fontkit";
import { InvoiceDto } from "./dtos/invoice-dto";
import { InvoiceLineDto } from "./dtos/invoice-line-dto";
import { PartyDto } from "./dtos/party-dto";
import { PaymentMeansDto } from "./dtos/payment-means-dto";
import { PdfFontCache } from "./pdf-font-cache";
import { PdfA3Converter } from "./pdf-a3-converter";

const TRANSLATIONS = {
  de: {
    invoice: "Rechnung",
    date: "Datum",
    phone: "Telefon",
    email: "E-Mail",
    website: "Internet",
    netTotal: "Summe Netto",
    tax: "Zzgl. USt.",
    total: "Gesamtsumme",
    bestRegards: "Mit freundlichen Grüßen",
    invoiceNumber: "Rechnungsnummer",
    start: "Start",
    end: "Ende",
    duration: "Dauer/h",
    activity: "Tätigkeit",
    ustId: "USt-IdNr.",
    stNr: "St.-Nr.",
  },
  en: {
    invoice: "Invoice",
    date: "Date",
    phone: "Phone",
    email: "Email",
    website: "Website",
    netTotal: "Net total",
    tax: "VAT",
    total: "Total",
    bestRegards: "Best regards",
    invoiceNumber: "Invoice number",
    start: "Start",
    end: "End",
    duration: "Dur./h",
    activity: "Activity",
    ustId: "VAT ID",
    stNr: "TAX Nr.",
  },
  fr: {
    invoice: "Facture",
    date: "Date",
    phone: "Téléphone",
    email: "Email",
    website: "Site web",
    netTotal: "Total HT",
    tax: "TVA",
    total: "Total TTC",
    bestRegards: "Cordialement",
    invoiceNumber: "Numéro de facture",
    start: "Début",
    end: "Fin",
    duration: "Durée/h",
    activity: "Activité",
    ustId: "N° de TVA",
    stNr: "N° fiscal",

  },
  es: {
    invoice: "Factura",
    date: "Fecha",
    phone: "Teléfono",
    email: "Correo electrónico",
    website: "Sitio web",
    netTotal: "Total neto",
    tax: "IVA",
    total: "Total",
    bestRegards: "Atentamente",
    invoiceNumber: "Número de factura",
    start: "Inicio",
    end: "Fin",
    duration: "Dur./h",
    activity: "Actividad",
    ustId: "NIF-IVA",
    stNr: "NIF",
  },
};


export class PdfGenerator {
  private marginX: number;
  private marginY: number;
  private defaultFontSize: number;
  private document: PDFDocument;
  private fonts: {
    default: PDFFont;
    bold: PDFFont;
    italic: PDFFont;
  };
  private fontCache: PdfFontCache;
  private logo?: PDFImage;
  private languageCode: string;

  constructor(
    document: PDFDocument,
    defaultFont: PDFFont,
    boldFont: PDFFont,
    italicFont: PDFFont,
    languageCode: string = 'de'
  ) {
    this.marginX = 50;
    this.marginY = 60;
    this.defaultFontSize = 12;
    this.document = document;
    this.fonts = {
      default: defaultFont,
      bold: boldFont,
      italic: italicFont,
    };
    this.fontCache = new PdfFontCache();
    this.languageCode = languageCode;
  }

  private t(key: string): string {
    return TRANSLATIONS[this.languageCode][key] || key;
  }

  public async generateInvoice(invoice: InvoiceDto): Promise<Uint8Array> {
    if (invoice.sellerParty.logoReferenceId) {
      const doc = invoice.additionalDocumentReferences.find(
        (f) => f.id === invoice.sellerParty.logoReferenceId,
      );
      if (doc) {
        this.logo = await this.embedImage(doc.content);
      }
    }
    let page = this.document.addPage(PageSizes.A4);
    this.drawHeader(page, invoice);
    this.drawFooter(page, invoice);
    this.drawSellerContact(page, invoice.sellerParty);
    this.drawBuyer(page, invoice.buyerParty);
    let lineY = this.drawNote(page, invoice);
    lineY -= 10;

    let remainingLines = invoice.invoiceLines.sort((a, b) => {
      const idA = parseInt(a.id, 10);
      const idB = parseInt(b.id, 10);
      if (isNaN(idA) || isNaN(idB)) {
        return a.id.localeCompare(b.id);
      }
      return idA - idB;
    });

    let currentY = lineY;
    while (remainingLines.length > 0) {
      ({ remainingLines, y: currentY } = this.drawInvoiceLines(
        page,
        remainingLines,
        lineY,
      ));
      if (remainingLines.length > 0) {
        page = this.document.addPage(PageSizes.A4);
        this.drawHeader(page, invoice);
        this.drawFooter(page, invoice);
        lineY = page.getHeight() - this.marginY - 80;
      }
    }

    if (currentY < this.marginY + 120 + 45) {
      page = this.document.addPage(PageSizes.A4);
      this.drawHeader(page, invoice);
      this.drawFooter(page, invoice);
      currentY = page.getHeight() - this.marginY - 80;
    }
    this.drawSummary(page, invoice, currentY);

    const pageCount = this.document.getPageCount();
    let i = 1;
    this.document.getPages().forEach((docPage) => {
      this.drawPageNumber(docPage, i, pageCount);
      i += 1;
    });

    const pdfBytes = await this.document.save();
    return pdfBytes;
  }

  private drawFooter(page: PDFPage, invoice: InvoiceDto): void {
    let { x, y } = this.calculateStartPosition(page, page.getWidth() / 2, 0);
    y = this.marginY + 25;
    x -= 25;
    this.drawText(page, invoice.sellerParty.name, this.marginX, y);
    this.drawText(page, invoice.paymentMeans.name, x, y);
    y -= 14.5;
    this.drawText(
      page,
      this.t("ustId") + ": " + invoice.sellerParty.taxId,
      this.marginX,
      y,
    );
    this.drawText(page, this.t("BIC") + ":  " + invoice.paymentMeans.bic, x, y);
    y -= 14.5;
    this.drawText(
      page,
      this.t("stNr") + ".: " + invoice.sellerParty.companyId,
      this.marginX,
      y,
    );
    if (invoice.paymentMeans.iban) {
      this.drawText(
        page,
        this.t("IBAN") + ": " + this.formatIBAN(invoice.paymentMeans.iban),
        x,
        y,
      );
    }
  }

  private drawHeader(page: PDFPage, invoice: InvoiceDto): void {
    let { x, y } = this.calculateStartPosition(page, page.getWidth() / 2, 0);

    this.drawText(page, invoice.sellerParty.name, x, y, 16, this.fonts.bold);
    this.drawText(page, invoice.sellerParty.registrationName, x, y - 18);
    if (this.logo) {
      const lwidth = Math.max(
        this.fontCache.getTextWidth(
          this.fonts.bold,
          invoice.sellerParty.name,
          16,
        ),
        this.fontCache.getTextWidth(
          this.fonts.default,
          invoice.sellerParty.registrationName,
          this.defaultFontSize,
        ),
      );
      this.addImageToPdf(page, this.logo, x + lwidth, y);
    }

    x = this.marginX;
    y -= 50;
    const adr = `${invoice.sellerParty.name} - ${invoice.sellerParty.streetName} - ${invoice.sellerParty.postCode} ${invoice.sellerParty.city}`;
    this.drawText(page, adr, x, y);
    const formattedDate = new Intl.DateTimeFormat(this.languageCode, {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    }).format(new Date(invoice.issueDate));
    this.drawText(page, `${this.t("date")}: ${formattedDate}`, page.getWidth() - 200, y);
  }

  private drawPageNumber(page: PDFPage, number: number, total: number): void {
    const pageInfo = `${number.toString()}/${total.toString()}`;
    const width = this.fontCache.getTextWidth(
      this.fonts.default,
      pageInfo,
      this.defaultFontSize,
    );
    const x = page.getWidth() - this.marginX - width;
    const y = page.getHeight() - this.marginY - 50;
    this.drawText(page, pageInfo, x, y);
  }

  private drawSellerContact(page: PDFPage, seller: PartyDto): void {
    let { x, y } = this.calculateStartPosition(
      page,
      page.getWidth() / 2,
      this.marginY * 2 - 30,
    );

    this.drawText(page, `${this.t("phone")}: ${seller.telefone}`, x, y);
    y -= 14;
    this.drawText(page, `${this.t("email")}: ${seller.email}`, x, y);
    y -= 14;
    if (seller.website) {
      this.drawText(page, `${this.t("website")}: ${seller.website}`, x, y);
    }
  }

  private drawBuyer(page: PDFPage, buyer: PartyDto): void {
    let { x, y } = this.calculateStartPosition(page, 0, this.marginY * 2 - 30);

    this.drawText(page, buyer.registrationName, x, y, 14);
    this.drawText(page, buyer.streetName, x, y - 20, 14);
    this.drawText(page, `${buyer.postCode} ${buyer.city}`, x, y - 36, 14);
  }

  private drawNote(page: PDFPage, invoice: InvoiceDto): number {
    let { x, y } = this.calculateStartPosition(page, 0, this.marginY * 3 - 20);

    this.drawText(page, this.t("invoice"), x, y, 14, this.fonts.bold);
    y -= 20;
    
    if (invoice.note) {
      const noteLines = invoice.note.split(/\r?\n/).map((line) => line.trim());
      noteLines.forEach((line) => {
        this.drawText(page, line, x, y);
        y -= 14;
      });
      y -= 16;
    }
    
    this.drawText(
      page,
      `${this.t("invoiceNumber")}: ${invoice.id}`,
      x,
      y,
      this.defaultFontSize,
      this.fonts.bold,
    );
    return y - 16;
  }

  private drawInvoiceLines(
    page: PDFPage,
    sortedLines: InvoiceLineDto[],
    startY: number,
  ): { remainingLines: InvoiceLineDto[]; y: number } {
    const x = this.marginX;
    let y = startY;
    const widthEnt = (page.getWidth() - 2 * this.marginX) / 100;
    const dateX = x;
    const descX = x + widthEnt * 15;
    const startX = descX + widthEnt * 45;
    const endX = startX + widthEnt * 10;
    const durX = endX + widthEnt * 10;

    const priceAmount =
      sortedLines.length > 0 ? `${sortedLines[0].unitPrice} €/h` : "€/h";
    const priceAmountWidth = this.fontCache.getTextWidth(
      this.fonts.bold,
      priceAmount,
      this.defaultFontSize,
    );
    const amountX = page.getWidth() - this.marginX - priceAmountWidth;

    const availableDescWidth = startX - descX;
    const lineHeight = 12;
    const yBreakCondition = 120;
    let remainingLines: InvoiceLineDto[] = [];

    // header
    this.drawText(page, this.t("date"), dateX, y, 12, this.fonts.bold);
    this.drawText(page, this.t("activity"), descX, y, 12, this.fonts.bold);
    this.drawText(page, this.t("start"), startX, y, 12, this.fonts.bold);
    this.drawText(page, this.t("end"), endX, y, 12, this.fonts.bold);
    this.drawText(page, this.t("duration"), durX, y, 12, this.fonts.bold);
    this.drawText(page, priceAmount, amountX, y, 12, this.fonts.bold);
    y -= 14.5;
    for (const line of sortedLines) {
      if (y <= yBreakCondition) {
        remainingLines = sortedLines.slice(sortedLines.indexOf(line));
        break;
      }
      const wrappedText = this.wrapText(
        line.name,
        availableDescWidth,
        this.defaultFontSize,
      );

      this.drawText(page, this.getDate(line.startDate, line.endDate), dateX, y);
      this.drawText(page, this.getTime(line.startDate), startX, y);
      this.drawText(page, this.getTime(line.endDate), endX, y);
      this.drawText(page, this.formatHours(line.quantity), durX, y);

      const formattedAmount = this.formatCurrency(line.lineTotal);
      const textWidth = this.fontCache.getTextWidth(
        this.fonts.default,
        formattedAmount,
        this.defaultFontSize,
      );
      this.drawText(
        page,
        formattedAmount,
        page.getWidth() - this.marginX - textWidth,
        y,
      );

      for (let i = 0; i < wrappedText.length; i++) {
        this.drawText(page, wrappedText[i], descX, y);
        y -= lineHeight;
      }
    }
    return { remainingLines, y };
  }

  private drawSummary(page: PDFPage, invoice: InvoiceDto, y: number): void {
    const pageWidth = page.getWidth();

    const taxRate = this.roundAmount(invoice.globalTax / 100.0);
    const taxExclusiveAmount = this.roundAmount(
      invoice.invoiceLines.reduce((a, c) => a + c.lineTotal, 0),
    );
    const payableAmount = this.roundAmount(
      taxExclusiveAmount + taxExclusiveAmount * taxRate,
    );
    const taxAmount = this.roundAmount(payableAmount - taxExclusiveAmount);

    const entries = [
      {
        label: this.t("netTotal") + "",
        value: this.formatCurrency(taxExclusiveAmount),
        font: this.fonts.bold,
      },
      {
        label: `${this.t("tax")} ${invoice.globalTax}%`,
        value: this.formatCurrency(taxAmount),
        font: this.fonts.default,
      },
      {
        label: this.t("total") + "",
        value: this.formatCurrency(payableAmount),
        font: this.fonts.bold,
      },
    ];

    const maxLabelWidth = Math.max(
      ...entries.map((entry) =>
        this.fontCache.getTextWidth(
          entry.font,
          entry.label,
          this.defaultFontSize,
        ),
      ),
    );
    const maxValueWidth = Math.max(
      ...entries.map((entry) =>
        this.fontCache.getTextWidth(
          entry.font,
          entry.value,
          this.defaultFontSize,
        ),
      ),
    );

    const valueX = pageWidth - this.marginX;
    const labelX = valueX - maxValueWidth - 10 - maxLabelWidth;

    page.drawLine({
      start: { x: pageWidth - 225, y },
      end: { x: pageWidth - this.marginX, y },
      thickness: 2,
    });
    y -= 14.5;

    for (const { label, value, font } of entries) {
      const labelWidth = this.fontCache.getTextWidth(
        font,
        label,
        this.defaultFontSize,
      );
      const adjustedLabelX = labelX + (maxLabelWidth - labelWidth);
      const valueWidth = this.fontCache.getTextWidth(
        font,
        value,
        this.defaultFontSize,
      );
      const adjustedValueX = valueX - valueWidth;

      this.drawText(page, label, adjustedLabelX, y, this.defaultFontSize, font);
      this.drawText(page, value, adjustedValueX, y, this.defaultFontSize, font);
      y -= 14.5;
    }

    y -= 20;
    this.drawText(page, invoice.paymentTermsNote, this.marginX, y);
    y -= 20;
    this.drawText(page, this.t("bestRegards"), this.marginX, y);
    y -= 14.5;
    this.drawText(page, invoice.sellerParty.name, this.marginX, y);
  }

  private calculateStartPosition(
    page: PDFPage,
    offsetX: number,
    offsetY: number,
  ): { x: number; y: number } {
    const width = page.getWidth();
    const height = page.getHeight();
    return {
      x: this.marginX + offsetX,
      y: height - this.marginY - offsetY,
    };
  }

  private async embedImage(base64String: string): Promise<PDFImage> {
    const imageBytes = Uint8Array.from(atob(base64String), (char) =>
      char.charCodeAt(0),
    );
    return await this.document.embedPng(imageBytes);
  }

  private async addImageToPdf(
    page: PDFPage,
    image: PDFImage,
    x: number,
    y: number,
  ) {
    const fixedHeight = 100;
    const aspectRatio = image.width / image.height;
    const calculatedWidth = fixedHeight * aspectRatio;
    page.drawImage(image, {
      x: x - 10,
      y: y - 5 - fixedHeight / 2,
      width: calculatedWidth,
      height: fixedHeight,
      opacity: 0.75,
    });
  }

  private drawText(
    page: PDFPage,
    text: string | null | undefined,
    x: number,
    y: number,
    size = this.defaultFontSize,
    font?: PDFFont,
  ): void {
    if (!text) {
      return;
    }
    if (!font) {
      font = this.fonts.default;
    }
    const sanitizedText = this.fontCache.sanitizeText(font, text);
    page.drawText(sanitizedText, { x, y, size, font });
  }

  private wrapText(text: string, maxWidth: number, fontSize: number): string[] {
    if (!text.trim()) {
      return [""];
    }
    const requiredWidth = this.fontCache.getTextWidth(
      this.fonts.default,
      text,
      fontSize,
    );
    if (requiredWidth <= maxWidth) {
      return [text];
    }

    const words = text.split(" ");
    const lines: string[] = [];
    let currentLine = "";

    words.forEach((word) => {
      const testLine = currentLine ? `${currentLine} ${word}` : word;
      const testWidth = this.fontCache.getTextWidth(
        this.fonts.default,
        testLine,
        fontSize,
      );

      if (testWidth <= maxWidth) {
        currentLine = testLine;
      } else {
        if (currentLine) {
          lines.push(currentLine);
        }
        currentLine = word;
      }
    });

    if (currentLine) {
      lines.push(currentLine);
    }

    return lines;
  }

  private formatHours(hours: number): string {
    const totalMinutes = Math.round(hours * 60);
    const h = Math.floor(totalMinutes / 60)
      .toString()
      .padStart(2, "0");
    const m = (totalMinutes % 60).toString().padStart(2, "0");
    return `${h}:${m}`;
  }

  private formatCurrency(amount: number, currency: string = "€"): string {
    const str = new Intl.NumberFormat(this.languageCode, {
      style: "currency",
      currency: "EUR",
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
      .format(amount)
      .replace("€", currency);
    return str.replace(/\u202F|\u00A0/g, ' ');
  }

  private formatIBAN(iban: string): string {
    // Remove existing spaces and uppercase it
    const cleanIban = iban.replace(/\s+/g, "").toUpperCase();
    // Group into blocks of 4
    return cleanIban.match(/.{1,4}/g)?.join(" ") ?? cleanIban;
  }

  private roundAmount(value: number): number {
    const factor = Math.pow(10, 2);
    return (Math.sign(value) * Math.round(Math.abs(value) * factor)) / factor;
  }
  private getTime(isoString: string | null): string {
    if (!isoString) {
      return "";
    }
    return new Date(isoString).toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
  }

  private getDate(startDate: string | null, endDate: string | null): string {
    if (!startDate || !endDate) {
      return "";
    }
    // if startDate and endDate are on the same day print dd.MM.yyyy
    const start = new Date(startDate);
    const end = new Date(endDate);
    if (
      start.getDate() === end.getDate() &&
      start.getMonth() === end.getMonth() &&
      start.getFullYear() === end.getFullYear()
    ) {
      return new Intl.DateTimeFormat(this.languageCode, {
        day: "2-digit",
        month: "2-digit",
        year: "2-digit",
      }).format(start);
      // if startDate and endDate are on the same month print dd-dd.MM.yyyy
    } else if (
      start.getMonth() === end.getMonth() &&
      start.getFullYear() === end.getFullYear()
    ) {
      return `${new Intl.DateTimeFormat(this.languageCode, {
        day: "2-digit",
      }).format(start)}-${new Intl.DateTimeFormat(this.languageCode, {
        day: "2-digit",
        month: "2-digit",
        year: "2-digit",
      }).format(end)}`;
    } else {
      return `${new Intl.DateTimeFormat(this.languageCode, {
        day: "2-digit",
        month: "2-digit",
      }).format(start)}-${new Intl.DateTimeFormat(this.languageCode, {
        day: "2-digit",
        month: "2-digit",
      }).format(end)}`;
    }
  }
}

function generateObjectUrl(pdfBytes: Uint8Array): string {
  const blob = new Blob([pdfBytes], { type: "application/pdf" });
  return URL.createObjectURL(blob);
}

export async function createPdf(): Promise<string> {
  const pdfDoc = await PDFDocument.create();
  const defaultFont = await pdfDoc.embedFont(StandardFonts.Helvetica);
  const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
  const italicFont = await pdfDoc.embedFont(StandardFonts.HelveticaOblique);

  const pdfGenerator = new PdfGenerator(
    pdfDoc,
    defaultFont,
    boldFont,
    italicFont,
  );

  const invoice = sampleInvoice();
  const logo =
    "iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==";
  // invoice.sellerParty.logo = { base64String: logo };
  const pdf = await pdfGenerator.generateInvoice(invoice);
  return generateObjectUrl(pdf);
}

export async function createInvoicePdfBytes(invoice: InvoiceDto, locale: string): Promise<Uint8Array> {
    const pdfDoc = await PDFDocument.create();
    const defaultFont = await pdfDoc.embedFont(StandardFonts.Helvetica);
    const boldFont = await pdfDoc.embedFont(StandardFonts.HelveticaBold);
    const italicFont = await pdfDoc.embedFont(StandardFonts.HelveticaOblique);

    const pdfGenerator = new PdfGenerator(
        pdfDoc,
        defaultFont,
        boldFont,
        italicFont,
        locale,
    );
    return await pdfGenerator.generateInvoice(invoice);
}

export async function createInvoicePdf(invoice: InvoiceDto, locale: string): Promise<string> {
  const pdf = await createInvoicePdfBytes(invoice, locale);
  return generateObjectUrl(pdf);
}

async function embedA3Fonts(pdfDoc: PDFDocument): Promise<{
  regular: PDFFont;
  bold: PDFFont;
  italic: PDFFont;
}> {
    pdfDoc.registerFontkit(fontkit as Fontkit);

    const fontUrls = [
        './_content/BlazorInvoice.Pdf/fonts/Inter-Light.ttf',
        './_content/BlazorInvoice.Pdf/fonts/Inter-Bold.ttf',
        './_content/BlazorInvoice.Pdf/fonts/Inter-MediumItalic.ttf'
    ];

    const fontResponses = await Promise.all(fontUrls.map(url => fetch(url)));
    const fontBuffers = await Promise.all(fontResponses.map(res => res.arrayBuffer()));

    const regularFont = await pdfDoc.embedFont(fontBuffers[0], { subset: true });
    const boldFont = await pdfDoc.embedFont(fontBuffers[1], { subset: true });
    const italicFont = await pdfDoc.embedFont(fontBuffers[2], { subset: true });
    return {
      regular: regularFont,
      bold: boldFont,
      italic: italicFont,
  };
}

export async function createInvoicePdfA3Bytes(invoice: InvoiceDto, locale: string, hexId: string, xmlText: string)
  : Promise<Uint8Array> {
    const pdfDoc = await PDFDocument.create();
    const fonts = await embedA3Fonts(pdfDoc);

    const pdfGenerator = new PdfGenerator(
        pdfDoc,
        fonts.regular,
        fonts.bold,
        fonts.italic,
        locale,
    );
    await pdfGenerator.generateInvoice(invoice);
    const pdfA3Converter = new PdfA3Converter();
    return await pdfA3Converter.createA3Pdf(pdfDoc, invoice, locale, hexId, xmlText);
}

export async function createInvoicePdfA3(invoice: InvoiceDto, locale: string, hexId: string, xmlText: string)
  : Promise<string> {
  const pdf = await createInvoicePdfA3Bytes(invoice, locale, hexId, xmlText);
  return generateObjectUrl(pdf);
}

export async function getPdfXmlText(pdfBytes: Uint8Array): Promise<string | null> {
  const pdfA3Converter = new PdfA3Converter();
  return await pdfA3Converter.getXmlString(pdfBytes);
}

const sampleSeller = (): PartyDto => {
  const seller: PartyDto = {
    email: "test@email.com",
    name: "Test Seller Name",
    streetName: "Test Street 12",
    city: "TestCity",
    postCode: "123456",
    countryCode: "DE",
    telefone: "54321",
    taxId: "DE123456789",
    companyId: "123/1234/1234/9",
    registrationName: "Test Seller Registration Name",
  };
  return seller;
};

const sampleBuyer = (): PartyDto => {
  const buyer: PartyDto = {
    email: "buyer@test.com",
    name: "Test Buyer",
    city: "Test City",
    postCode: "12345",
    countryCode: "DE",
    streetName: "Test Street 21",
    telefone: "",
    registrationName: "",
    taxId: "",
    companyId: "",
  };
  return buyer;
};

export const sampleInvoice = (): InvoiceDto => {
  const invoice: InvoiceDto = {
    id: "20250101-1",
    issueDate: new Date(2025, 1, 1).toISOString(),
    dueDate: new Date(2025, 1, 14).toISOString(),
    note: "Sehr geehrte Damen und Herren,\nhiermit stelle ich Ihnen wie vereinbart, folgende Leistungen im Kontext\nder Test-Aufgabe in Rechnung.",
    invoiceTypeCode: "380",
    documentCurrencyCode: "EUR",
    buyerReference: "buyer@test.com",
    sellerParty: sampleSeller(),
    buyerParty: sampleBuyer(),
    paymentMeans: samplePaymentMeans(),
    invoiceLines: [],
    globalTaxCategory: "S",
    globalTaxScheme: "VAT",
    globalTax: 19,
    additionalDocumentReferences: [],
    paymentTermsNote:
      "Zahlbar innerhalb von 14 Tagen auf unten stehendes Konto.",
    payableAmount: 0,
  };
  for (let i = 1; i < 73; i++) {
    invoice.invoiceLines.push(sampleInvoiceLine(i.toString()));
  }
  invoice.invoiceLines[2].name =
    "Very long name of the service delivered. This is for testing only.";
  invoice.invoiceLines[2].quantity = 2.5;
  invoice.invoiceLines[2].unitPrice = 10;
  return invoice;
};

const sampleInvoiceLine = (id: string): InvoiceLineDto => {
  const line: InvoiceLineDto = {
    id,
    quantity: 3,
    quantityCode: "HUR",
    name: `Test Name ${id}`,
    description: "28.12.2024",
    startDate: new Date(2025, 1, 1, 8, 0, 0).toISOString(),
    endDate: new Date(2025, 1, 1, 11, 0, 0).toISOString(),
    lineTotal: 60.0,
    unitPrice: 20,
  };
  return line;
};

const samplePaymentMeans = (): PaymentMeansDto => {
  const payment: PaymentMeansDto = {
    iban: "0000 0000 0000 0000 0000 00",
    bic: "00000000000",
    name: "Test Bank",
  };
  return payment;
};
