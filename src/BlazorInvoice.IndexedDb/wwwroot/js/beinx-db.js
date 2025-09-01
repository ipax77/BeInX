import pako from "./pako";
const DB_NAME = "BeInXDB";
const DB_VERSION = 1;
const STORES = {
    invoices: "Invoices",
    parties: "Parties",
    payments: "PaymentMeans",
    references: "AdditionalDocumentReferences",
    config: "AppConfig",
};
let db = null;
export function openDB() {
    return new Promise((resolve, reject) => {
        if (db) {
            resolve(db);
            return;
        }
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = (event) => {
            const database = event.target.result;
            if (!database.objectStoreNames.contains(STORES.invoices)) {
                database.createObjectStore(STORES.invoices, { keyPath: "id" });
            }
            if (!database.objectStoreNames.contains(STORES.parties)) {
                database.createObjectStore(STORES.parties, { keyPath: "email" });
            }
            if (!database.objectStoreNames.contains(STORES.payments)) {
                database.createObjectStore(STORES.payments, { keyPath: "iban" });
            }
            if (!database.objectStoreNames.contains(STORES.references)) {
                database.createObjectStore(STORES.references, { keyPath: "id" });
            }
            if (!database.objectStoreNames.contains(STORES.config)) {
                database.createObjectStore(STORES.config);
            }
        };
        request.onsuccess = () => {
            db = request.result;
            resolve(db);
        };
        request.onerror = () => reject(request.error);
    });
}
export async function getConfig() {
    const database = await openDB();
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readonly");
        const store = tx.objectStore(STORES.config);
        const request = store.get("app");
        request.onsuccess = () => {
            resolve(request.result);
        };
        request.onerror = () => reject(request.error);
    });
}
export async function saveConfig(config) {
    const database = await openDB();
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readwrite");
        const configs = tx.objectStore(STORES.config);
        configs.put(config, "app");
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}
export async function downloadBackup() {
    const base64 = await exportDb();
    const blob = new Blob([base64], { type: "text/plain" });
    const timestamp = new Date()
        .toISOString()
        .replace(/[:.]/g, "-"); // safe filename: 2025-08-30T20-15-00-123Z
    const filename = `replaydb-backup-${timestamp}.json.gz.txt`;
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}
export async function exportDb() {
    const database = await openDB();
    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readonly");
        const dump = {};
        let pending = database.objectStoreNames.length;
        if (pending === 0)
            resolve("");
        for (const storeName of Array.from(database.objectStoreNames)) {
            const store = tx.objectStore(storeName);
            const req = store.getAll();
            req.onsuccess = () => {
                dump[storeName] = req.result;
                if (--pending === 0) {
                    const base64 = gzipString(JSON.stringify(dump));
                    resolve(base64);
                }
            };
            req.onerror = () => reject(req.error);
        }
    });
}
/**
 * Open file picker, read backup file and import into DB
 */
export async function uploadBackup(replace = false) {
    return new Promise((resolve, reject) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = ".txt,.gz,.json";
        input.onchange = async () => {
            if (!input.files || input.files.length === 0)
                return reject("No file selected");
            const file = input.files[0];
            const reader = new FileReader();
            reader.onload = async () => {
                try {
                    const base64 = reader.result;
                    await importDb(base64, replace);
                    resolve();
                }
                catch (err) {
                    reject(err);
                }
            };
            reader.onerror = () => reject(reader.error);
            reader.readAsText(file);
        };
        input.click();
    });
}
export async function importDb(base64, replace = false) {
    const database = await openDB();
    const json = ungzipString(base64);
    const dump = JSON.parse(json);
    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readwrite");
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
        for (const [storeName, items] of Object.entries(dump)) {
            if (!database.objectStoreNames.contains(storeName))
                continue;
            const store = tx.objectStore(storeName);
            if (replace) {
                const clearReq = store.clear();
                clearReq.onsuccess = () => {
                    for (const item of items)
                        store.put(item);
                };
            }
            else {
                for (const item of items)
                    store.put(item);
            }
        }
    });
}
export function gzipString(content) {
    const binary = pako.gzip(content);
    return btoa(String.fromCharCode(...binary));
}
export function ungzipString(base64) {
    const binary = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const text = pako.ungzip(binary, { to: "string" });
    return text;
}
//# sourceMappingURL=beinx-db.js.map