import * as pako from "pako";
import { openDB, STORES } from "./db-core";
import { PaymentRepository } from "./payment-repository";
import { AppConfigDto } from "./dtos";
import { DraftRepository } from "./draft-repository";
import { PartyRepository } from "./party-repository";

export async function getConfig(): Promise<AppConfigDto | undefined> {
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

export async function saveConfig(
    config: AppConfigDto,
): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readwrite");

        const configs = tx.objectStore(STORES.config);
        configs.put(config, "app");

        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

export async function downloadBackup(): Promise<void> {
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

export async function exportDb(): Promise<string> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readonly");
        const dump: Record<string, unknown[]> = {};

        let pending = database.objectStoreNames.length;
        if (pending === 0) resolve("");

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

export async function uploadBackup(replace: boolean = false): Promise<void> {
    return new Promise((resolve, reject) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = ".txt,.gz,.json";

        input.onchange = async () => {
            if (!input.files || input.files.length === 0) return reject("No file selected");

            const file = input.files[0];
            const reader = new FileReader();

            reader.onload = async () => {
                try {
                    const base64 = reader.result as string;
                    await importDb(base64, replace);
                    resolve();
                } catch (err) {
                    reject(err);
                }
            };

            reader.onerror = () => reject(reader.error);
            reader.readAsText(file);
        };

        input.click();
    });
}

export async function importDb(base64: string, replace: boolean = false): Promise<void> {
    const database = await openDB();

    const json = ungzipString(base64);
    const dump: Record<string, unknown[]> = JSON.parse(json);

    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readwrite");
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);

        for (const [storeName, items] of Object.entries(dump)) {
            if (!database.objectStoreNames.contains(storeName)) continue;
            const store = tx.objectStore(storeName);

            if (replace) {
                const clearReq = store.clear();
                clearReq.onsuccess = () => {
                    for (const item of items) store.put(item);
                };
            } else {
                for (const item of items) store.put(item);
            }
        }
    });
}

export function gzipString(content: string): string {
    const binary = pako.gzip(content);
    return btoa(String.fromCharCode(...binary));
}

export function ungzipString(base64: string): string {
    const binary = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const text = pako.ungzip(binary, { to: "string" });
    return text;
}

export const paymentRepository = new PaymentRepository();
export const partyRepository = new PartyRepository();
export const draftRepository = new DraftRepository();

