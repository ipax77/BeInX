import { AppConfigDto, InvoiceListRequest, IPaymentMeansBaseDto, PaymentListDto } from "./dtos.js";
import { InvoiceRepository } from "./invoice.repository.js";
import pako from "./pako/index.js";
import { PartyRepository } from "./party-repository.js";

const DB_NAME = "BeInXDB";
const DB_VERSION = 1;

export const STORES = {
    invoices: "Invoices",
    parties: "Parties",
    payments: "PaymentMeans",
    references: "AdditionalDocumentReferences",
    config: "AppConfig",
    temp_invoices: "TempInvoices",
};

let db: IDBDatabase | null = null;

export function openDB(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        if (db) {
            resolve(db);
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const database = (event.target as IDBOpenDBRequest).result;

           if (!database.objectStoreNames.contains(STORES.invoices)) {
                const invoiceStore = database.createObjectStore(STORES.invoices, { 
                    keyPath: "id", 
                    autoIncrement: true 
                });
                
                // Create indexes for common queries
                invoiceStore.createIndex("invoiceId", "invoiceId", { unique: true });
                invoiceStore.createIndex("sellerPartyId", "sellerPartyId");
                invoiceStore.createIndex("buyerPartyId", "buyerPartyId");
                invoiceStore.createIndex("paymentMeansId", "paymentMeansId");
                invoiceStore.createIndex("isPaid", "isPaid");
                invoiceStore.createIndex("isDeleted", "isDeleted");
                invoiceStore.createIndex("issueDate", "issueDate");
            }

            if (!database.objectStoreNames.contains(STORES.parties)) {
                database.createObjectStore(STORES.parties, { keyPath: "id", autoIncrement: true });
            }

            if (!database.objectStoreNames.contains(STORES.payments)) {
                database.createObjectStore(STORES.payments, { keyPath: "id", autoIncrement: true });
            }

            if (!database.objectStoreNames.contains(STORES.references)) {
                database.createObjectStore(STORES.references, { keyPath: "id", autoIncrement: true });
            }

            if (!database.objectStoreNames.contains(STORES.config)) {
                database.createObjectStore(STORES.config);
            }

            if (!database.objectStoreNames.contains(STORES.temp_invoices)) {
                database.createObjectStore(STORES.temp_invoices);
            }
        };

        request.onsuccess = () => {
            db = request.result;
            resolve(db);
        };

        request.onerror = () => reject(request.error);
    });
}

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

/**
 * Open file picker, read backup file and import into DB
 */
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

/// Payments 

async function getPaymentListQueryable(request: InvoiceListRequest): Promise<PaymentListDto[]> {
    const db = await openDB();
    const transaction = db.transaction(STORES.payments, 'readonly');
    const store = transaction.objectStore(STORES.payments);
    const allPayments = await new Promise<any[]>((resolve, reject) => {
        const req = store.getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });

    let query = allPayments
        .filter(p => !p.isDeleted)
        .map(s => ({
            playmentMeansId: s.id,
            iban: s.iban,
            name: s.name,
        } as PaymentListDto));

    if (request.filter) {
        const filter = request.filter.toLowerCase();
        query = query.filter(i =>
            i.name.toLowerCase().includes(filter) ||
            i.iban.toLowerCase().includes(filter)
        );
    }

    return query;
}

export async function getPaymentsCount(request: InvoiceListRequest): Promise<number> {
    const query = await getPaymentListQueryable(request);
    return query.length;
}

export async function getPayments(request: InvoiceListRequest): Promise<PaymentListDto[]> {
    let query = await getPaymentListQueryable(request);

    if (request.tableOrders && request.tableOrders.length > 0) {
        const order = request.tableOrders[0];
        const key = order.propertyName.toLowerCase() as keyof PaymentListDto;
        query.sort((a, b) => {
            if (a[key] < b[key]) return order.ascending ? -1 : 1;
            if (a[key] > b[key]) return order.ascending ? 1 : -1;
            return 0;
        });
    } else {
        query.sort((a, b) => a.name.localeCompare(b.name));
    }

    return query.slice(request.skip, request.skip + request.take);
}

export async function getPaymentMeans(paymentMeansId: number): Promise<IPaymentMeansBaseDto> {
    const db = await openDB();
    const transaction = db.transaction(STORES.payments, 'readonly');
    const store = transaction.objectStore(STORES.payments);
    return new Promise((resolve, reject) => {
        const req = store.get(paymentMeansId);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

export async function createPaymentMeans(paymentMeans: IPaymentMeansBaseDto): Promise<number> {
    const db = await openDB();
    const transaction = db.transaction(STORES.payments, 'readwrite');
    const store = transaction.objectStore(STORES.payments);
    return new Promise((resolve, reject) => {
        const req = store.add(paymentMeans);
        req.onsuccess = () => resolve(req.result as number);
        req.onerror = () => reject(req.error);
    });
}

export async function updatePaymentMeans(paymentMeansId: number, paymentMeans: IPaymentMeansBaseDto): Promise<void> {
    const db = await openDB();
    const transaction = db.transaction(STORES.payments, 'readwrite');
    const store = transaction.objectStore(STORES.payments);
    return new Promise((resolve, reject) => {
        const req = store.put({ ...paymentMeans, id: paymentMeansId });
        req.onsuccess = () => resolve();
        req.onerror = () => reject(req.error);
    });
}

export async function deletePaymentMeans(paymentMeansId: number): Promise<void> {
    const db = await openDB();

    const invoicesTransaction = db.transaction(STORES.invoices, 'readonly');
    const invoicesStore = invoicesTransaction.objectStore(STORES.invoices);
    const allInvoices = await new Promise<any[]>((resolve, reject) => {
        const req = invoicesStore.getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });

    const isReferenced = allInvoices.some(invoice => invoice.paymentMeansId === paymentMeansId);

    const paymentsTransaction = db.transaction(STORES.payments, 'readwrite');
    const paymentsStore = paymentsTransaction.objectStore(STORES.payments);

    if (isReferenced) {
        const req = paymentsStore.get(paymentMeansId);
        req.onsuccess = () => {
            const payment = req.result;
            if (payment) {
                payment.isDeleted = true;
                paymentsStore.put(payment);
            }
        };
    } else {
        paymentsStore.delete(paymentMeansId);
    }

    return new Promise((resolve, reject) => {
        paymentsTransaction.oncomplete = () => resolve();
        paymentsTransaction.onerror = () => reject(paymentsTransaction.error);
    });
}

// Export a singleton instance
export const partyRepository = new PartyRepository();
export const invoiceRepository = new InvoiceRepository();