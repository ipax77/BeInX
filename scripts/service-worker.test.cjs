const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const dbVersion = require('../src/beinx.db/package.json').version;
const pdfVersion = require('../src/BlazorInvoice.Pdf/Typescript/package.json').version;

const source = fs.readFileSync(require('node:path').join(__dirname, '../src/beinx.pwa/wwwroot/service-worker.published.js'), 'utf8');

for (const base of ['/', '/BeInX/']) {
    test(`offline scripts and navigation at ${base}`, async () => {
        const scope = `https://example.test${base}`;
        const assets = ['index.html', '_content/beinx.web/js/blazorinvoice.js',
            '_content/beinx.web/js/chart.umd.min.js',
            `_content/beinx.db/js/beinx-db.bundle.v${dbVersion}.js`,
            `_content/BlazorInvoice.Pdf/dist/pdf-generator.v${pdfVersion}.js`];
        const cacheEntries = new Map();
        const listeners = {};
        const cache = {
            addAll: async requests => requests.forEach(request => cacheEntries.set(request.url, { url: request.url })),
            match: async request => cacheEntries.get(typeof request === 'string' ? new URL(request, scope).href : request.url),
        };
        class ScopedRequest {
            constructor(url) { this.url = new URL(url, scope).href; }
        }
        const context = vm.createContext({
            URL, Request: ScopedRequest, console: { info() {} },
            self: { registration: { scope }, assetsManifest: { version: 'test', assets: assets.map(url => ({ url, hash: 'test' })) },
                importScripts() {}, addEventListener: (name, handler) => listeners[name] = handler },
            caches: { open: async () => cache },
            fetch: async () => { throw new Error('Offline'); },
        });
        vm.runInContext(source, context);
        let installation;
        listeners.install({ waitUntil: promise => installation = promise });
        await installation;
        async function request(path, mode = 'cors') {
            let response;
            listeners.fetch({ request: { url: new URL(path, scope).href, method: 'GET', mode }, respondWith: promise => response = promise });
            return response;
        }
        for (const asset of assets.slice(1)) {
            assert.equal((await request(asset)).url, new URL(asset, scope).href);
            assert.equal((await request(`${asset}?v=0.6.0`)).url, new URL(asset, scope).href);
        }
        assert.equal((await request('invoices', 'navigate')).url, `${scope}index.html`);
        for (const path of ['missing.js?v=1', `${assets[1]}?lang=en`, `${assets[1]}?v=1&lang=en`,
            '_content/beinx.db/js/beinx-db.bundle.v0.0.0.js', `${assets[1]}?v=1&v=2`, 'index.html?v=1']) {
            await assert.rejects(request(path), /Offline/);
        }
        await assert.rejects(request(`https://other.test${base}${assets[1]}?v=1`), /Offline/);
    });
}

test('generated imports match the package versions and available bundles', () => {
    for (const [project, bundle, version] of [
        ['beinx.db', 'js/beinx-db.bundle', dbVersion],
        ['BlazorInvoice.Pdf', 'dist/pdf-generator', pdfVersion],
    ]) {
        const relativePath = `${bundle}.v${version}.js`;
        const projectPath = path.join(__dirname, '../src', project);
        assert.ok(fs.existsSync(path.join(projectPath, 'wwwroot', relativePath)));
        assert.ok(fs.readFileSync(path.join(projectPath, 'JsModule.g.cs'), 'utf8')
            .includes(`"_content/${project}/${relativePath}"`));
    }
});
