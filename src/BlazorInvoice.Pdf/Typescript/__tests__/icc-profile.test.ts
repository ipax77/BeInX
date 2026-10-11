import { vi } from 'vitest';

async function createLoader() {
    // Isolate the cache without adding a production reset API.
    vi.resetModules();
    const { PdfA3Converter } = await import('../pdf-a3-converter');
    class ProfileLoader extends PdfA3Converter {
        public load() { return this.loadIccProfile(); }
    }
    return ProfileLoader;
}

afterEach(() => vi.unstubAllGlobals());

it('shares a cached ICC profile across sequential converter instances', async () => {
    const Loader = await createLoader();
    const fetch = vi.fn().mockResolvedValue(new Response(new Uint8Array([1, 2, 3]).buffer));
    vi.stubGlobal('fetch', fetch);
    const first = await new Loader().load();
    const second = await new Loader().load();
    expect(first).toEqual(new Uint8Array([1, 2, 3]));
    expect(second).toBe(first);
    expect(fetch).toHaveBeenCalledTimes(1);
});

it('shares an in-flight ICC profile request across concurrent instances', async () => {
    const Loader = await createLoader();
    let resolve!: (response: Response) => void;
    const fetch = vi.fn(() => new Promise<Response>(done => { resolve = done; }));
    vi.stubGlobal('fetch', fetch);
    const first = new Loader().load();
    const second = new Loader().load();
    expect(fetch).toHaveBeenCalledTimes(1);
    resolve(new Response(new Uint8Array([4, 5]).buffer));
    const profiles = await Promise.all([first, second]);
    expect(profiles[0]).toEqual(new Uint8Array([4, 5]));
    expect(profiles[1]).toBe(profiles[0]);
});

it.each(['HTTP', 'network', 'body'])('clears a failed %s load and retries successfully', async failure => {
    const Loader = await createLoader();
    const fetch = vi.fn();
    if (failure === 'HTTP') fetch.mockResolvedValueOnce(new Response('Not found', { status: 404 }));
    if (failure === 'network') fetch.mockRejectedValueOnce(new Error('Network unavailable'));
    if (failure === 'body') fetch.mockResolvedValueOnce({ ok: true, arrayBuffer: () => Promise.reject(new Error('Body read failed')) });
    fetch.mockResolvedValueOnce(new Response(new Uint8Array([6, 7]).buffer));
    vi.stubGlobal('fetch', fetch);
    const outcomes = await Promise.allSettled([new Loader().load(), new Loader().load()]);
    expect(outcomes.map(outcome => outcome.status)).toEqual(['rejected', 'rejected']);
    if (failure === 'HTTP') expect((outcomes[0] as PromiseRejectedResult).reason.message).toContain('HTTP 404');
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(await new Loader().load()).toEqual(new Uint8Array([6, 7]));
    expect(fetch).toHaveBeenCalledTimes(2);
});
