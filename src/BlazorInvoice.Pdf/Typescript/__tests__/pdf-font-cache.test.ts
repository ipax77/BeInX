import { PDFFont, PDFRef } from 'pdf-lib';
import { vi } from 'vitest';
import { PdfFontCache } from '../pdf-font-cache';

function font(characters: string, width = 10, height = 12) {
    const methods = {
        // Different documents can assign the same reference to different fonts.
        ref: PDFRef.of(1),
        getCharacterSet: vi.fn(() => Array.from(characters, character => character.codePointAt(0)!)),
        widthOfTextAtSize: vi.fn(() => width),
        heightAtSize: vi.fn(() => height),
    };
    return { font: methods as unknown as PDFFont, methods };
}

it('keeps sanitation separate for fonts with different character support', () => {
    const cache = new PdfFontCache();
    const regular = font('A?');
    const bold = font('?');
    expect(cache.sanitizeText(regular.font, 'A')).toBe('A');
    expect(cache.sanitizeText(bold.font, 'A')).toBe('?');
    expect(cache.sanitizeText(regular.font, 'A')).toBe('A');
});

it.each(['constructor', 'toString', '__proto__'])('handles arbitrary cache key text: %s', text => {
    const cache = new PdfFontCache();
    const supported = font(text);
    expect(cache.sanitizeText(supported.font, text)).toBe(text);
    expect(cache.sanitizeText(supported.font, text)).toBe(text);
});

it('supports supplementary Unicode code points and replaces unsupported characters once', () => {
    const cache = new PdfFontCache();
    const supported = font('A𝄞?');
    expect(cache.sanitizeText(supported.font, 'A𝄞😀')).toBe('A𝄞?');
    expect(cache.sanitizeText(supported.font, '😀')).toBe('?');
});

it('reads supported characters once per font even for distinct strings', () => {
    const cache = new PdfFontCache();
    const supported = font('ABC?');
    for (const text of ['A', 'B', 'C', 'ABC', 'A', '']) cache.sanitizeText(supported.font, text);
    expect(supported.methods.getCharacterSet).toHaveBeenCalledTimes(1);
});

it('separates measurements by font identity and size, including zero values', () => {
    const cache = new PdfFontCache();
    const first = font('A?', 0, 0);
    const second = font('A?', 20, 24);
    for (let repeat = 0; repeat < 2; repeat++) {
        expect(cache.getTextWidth(first.font, 'A', 12)).toBe(0);
        expect(cache.getTextHeight(first.font, 12)).toBe(0);
        expect(cache.getTextWidth(second.font, 'A', 12)).toBe(20);
        expect(cache.getTextHeight(second.font, 12)).toBe(24);
    }
    expect(first.methods.widthOfTextAtSize).toHaveBeenCalledTimes(1);
    expect(first.methods.heightAtSize).toHaveBeenCalledTimes(1);
    expect(second.methods.widthOfTextAtSize).toHaveBeenCalledTimes(1);
    expect(second.methods.heightAtSize).toHaveBeenCalledTimes(1);
    cache.getTextWidth(first.font, 'A', 18);
    cache.getTextHeight(first.font, 18);
    expect(first.methods.widthOfTextAtSize).toHaveBeenCalledTimes(2);
    expect(first.methods.heightAtSize).toHaveBeenCalledTimes(2);
});

it('measures the sanitized text that will be drawn', () => {
    const cache = new PdfFontCache();
    const supported = font('A?');
    cache.getTextWidth(supported.font, 'A😀', 12);
    expect(supported.methods.widthOfTextAtSize).toHaveBeenCalledWith('A?', 12);
    cache.getTextWidth(supported.font, 'A?', 12);
    expect(supported.methods.widthOfTextAtSize).toHaveBeenCalledTimes(1);
});
