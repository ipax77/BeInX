import { PDFFont } from "pdf-lib";

interface FontCacheEntry {
  characters?: Set<number>;
  text: Map<string, string>;
  widths: Map<number, Map<string, number>>;
  heights: Map<number, number>;
}

export class PdfFontCache {
  private readonly fonts = new WeakMap<PDFFont, FontCacheEntry>();

  private getCache(font: PDFFont): FontCacheEntry {
    let cache = this.fonts.get(font);
    if (!cache) {
      cache = { text: new Map(), widths: new Map(), heights: new Map() };
      this.fonts.set(font, cache);
    }
    return cache;
  }

  public sanitizeText(font: PDFFont, text: string): string {
    const cache = this.getCache(font);
    const cached = cache.text.get(text);
    if (cached !== undefined) return cached;

    cache.characters ??= new Set(font.getCharacterSet());
    const characters = cache.characters;
    const sanitized = Array.from(text, character =>
      characters.has(character.codePointAt(0)!) ? character : "?",
    ).join("");
    cache.text.set(text, sanitized);
    cache.text.set(sanitized, sanitized);
    return sanitized;
  }

  public getTextWidth(font: PDFFont, text: string, size: number): number {
    const sanitized = this.sanitizeText(font, text);
    const cache = this.getCache(font);
    let widths = cache.widths.get(size);
    if (!widths) {
      widths = new Map();
      cache.widths.set(size, widths);
    }
    const cached = widths.get(sanitized);
    if (cached !== undefined) return cached;
    const width = font.widthOfTextAtSize(sanitized, size);
    widths.set(sanitized, width);
    return width;
  }

  public getTextHeight(font: PDFFont, size: number): number {
    const cache = this.getCache(font);
    const cached = cache.heights.get(size);
    if (cached !== undefined) return cached;
    const height = font.heightAtSize(size);
    cache.heights.set(size, height);
    return height;
  }
}
