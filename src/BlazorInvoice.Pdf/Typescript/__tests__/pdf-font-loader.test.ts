import { PDFDocument } from 'pdf-lib';
import { vi } from 'vitest';
import { documentId, invoiceXml, mockPdfAssets, readPageContent } from './pdf-test-helpers';

beforeEach(() => vi.resetModules());
afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

it('loads source bytes once across sequential requests', async () => {
    const { loadA3FontBytes } = await import('../pdf-font-loader');
    const fetch = vi.fn(async () => new Response(new Uint8Array([1, 2, 3]).buffer));
    vi.stubGlobal('fetch', fetch);
    const first = await loadA3FontBytes();
    const second = await loadA3FontBytes();
    expect(fetch).toHaveBeenCalledTimes(3);
    first.forEach((bytes, index) => {
        expect(bytes).toEqual(new Uint8Array([1, 2, 3]));
        expect(second[index]).toBe(bytes);
    });
});

it('shares in-flight font requests across concurrent calls', async () => {
    const { loadA3FontBytes } = await import('../pdf-font-loader');
    const resolvers: ((response: Response) => void)[] = [];
    const fetch = vi.fn(() => new Promise<Response>(resolve => resolvers.push(resolve)));
    vi.stubGlobal('fetch', fetch);
    const first = loadA3FontBytes();
    const second = loadA3FontBytes();
    expect(fetch).toHaveBeenCalledTimes(3);
    resolvers.forEach(resolve => resolve(new Response(new Uint8Array([4, 5]).buffer)));
    const results = await Promise.all([first, second]);
    results[0].forEach((bytes, index) => expect(results[1][index]).toBe(bytes));
});

it.each(['HTTP', 'network', 'body'])('retries only the font whose %s load failed', async failure => {
    const { loadA3FontBytes } = await import('../pdf-font-loader');
    let fail = true;
    const fetch = vi.fn(async (url: string) => {
        if (url.endsWith('Inter-Bold.ttf') && fail) {
            fail = false;
            if (failure === 'HTTP') return new Response('Not found', { status: 404 });
            if (failure === 'network') throw new Error('Network unavailable');
            return { ok: true, arrayBuffer: () => Promise.reject(new Error('Body read failed')) };
        }
        return new Response(new Uint8Array([6, 7]).buffer);
    });
    vi.stubGlobal('fetch', fetch);
    const outcomes = await Promise.allSettled([loadA3FontBytes(), loadA3FontBytes()]);
    expect(outcomes.map(outcome => outcome.status)).toEqual(['rejected', 'rejected']);
    if (failure === 'HTTP') {
        expect((outcomes[0] as PromiseRejectedResult).reason.message).toContain('Inter-Bold.ttf: HTTP 404');
    }
    const bytes = await loadA3FontBytes();
    bytes.forEach(font => expect(font).toEqual(new Uint8Array([6, 7])));
    expect(fetch).toHaveBeenCalledTimes(4);
    expect(fetch.mock.calls.filter(([url]) => url.endsWith('Inter-Bold.ttf'))).toHaveLength(2);
});

it('reuses source files while embedding independent fonts into each generated PDF', async () => {
    mockPdfAssets();
    const fetch = vi.mocked(globalThis.fetch);
    const { createInvoicePdfA3Bytes, sampleInvoice } = await import('../pdf-generator');
    const embed = vi.spyOn(PDFDocument.prototype, 'embedFont');
    const first = await createInvoicePdfA3Bytes(sampleInvoice(), 'de', documentId, invoiceXml);
    const second = await createInvoicePdfA3Bytes(sampleInvoice(), 'de', documentId, invoiceXml);
    expect(fetch.mock.calls.filter(([url]) => String(url).endsWith('.ttf'))).toHaveLength(3);
    expect(embed).toHaveBeenCalledTimes(6);
    for (let index = 0; index < 3; index++) {
        const firstFont = await embed.mock.results[index].value;
        const secondFont = await embed.mock.results[index + 3].value;
        // Compare identity without asking the matcher to inspect large fontkit graphs.
        expect(firstFont === secondFont).toBe(false);
    }
    for (const bytes of [first, second]) {
        const pages = await readPageContent(bytes);
        expect(pages.length).toBeGreaterThan(1);
        pages.forEach(page => expect(page).toMatch(/\bTj\b|\bTJ\b/));
    }
});
