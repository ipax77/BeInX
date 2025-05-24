
import { PDFFont } from "pdf-lib";

export class PdfFontCache {
  private textCache: Record<string, string> = {};
  private sanitizedCache: Record<string, boolean> = {};
  private widthCache: Record<string, number> = {};
  private heightCache: Record<string, number> = {};

  public sanitizeText(font: PDFFont, text: string): string {
    if (text in this.textCache) {
      return this.textCache[text];
    }

    if (text in this.sanitizedCache) {
      return text;
    }

    const characterSet = font.getCharacterSet();
    const isCharacterSupported = (char: string) =>
      characterSet.includes(char.charCodeAt(0));
    const sanitizedText = Array.from(text)
      .map((char) => (isCharacterSupported(char) ? char : "?"))
      .join("");

    this.textCache[text] = sanitizedText;
    this.sanitizedCache[sanitizedText] = true;

    return sanitizedText;
  }

  public getTextWidth(font: PDFFont, text: string, size: number) {
    const sanitizedText = this.sanitizeText(font, text);
    const fontRef = font.ref.toString();
    const cacheKey = `${fontRef}-${sanitizedText}-${size}`;

    if (cacheKey in this.widthCache) {
      return this.widthCache[cacheKey];
    }

    const width = font.widthOfTextAtSize(sanitizedText, size);

    this.widthCache[cacheKey] = width;

    return width;
  }

  public getTextHeight(font: PDFFont, size: number) {
    const fontRef = font.ref.toString();
    const cacheKey = `${fontRef}-${size}`;

    if (cacheKey in this.heightCache) {
      return this.heightCache[cacheKey];
    }

    const height = font.heightAtSize(size);

    this.heightCache[cacheKey] = height;

    return height;
  }
}
