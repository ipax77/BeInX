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