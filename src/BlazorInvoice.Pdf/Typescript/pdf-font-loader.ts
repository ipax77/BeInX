const fontUrls = [
    './_content/BlazorInvoice.Pdf/fonts/Inter-Light.ttf',
    './_content/BlazorInvoice.Pdf/fonts/Inter-Bold.ttf',
    './_content/BlazorInvoice.Pdf/fonts/Inter-MediumItalic.ttf',
];

// Cache source bytes only. Embedded fonts and subsets belong to each PDF document.
const fontBytes = new Map<string, Promise<Uint8Array>>();

export function loadA3FontBytes(): Promise<Uint8Array[]> {
    return Promise.all(fontUrls.map(url => {
        let pending = fontBytes.get(url);
        if (!pending) {
            pending = (async () => {
                const response = await fetch(url);
                if (!response.ok) throw new Error(`Failed to load font ${url}: HTTP ${response.status}`);
                return new Uint8Array(await response.arrayBuffer());
            })().catch(error => {
                fontBytes.delete(url);
                throw error;
            });
            fontBytes.set(url, pending);
        }
        return pending;
    }));
}
