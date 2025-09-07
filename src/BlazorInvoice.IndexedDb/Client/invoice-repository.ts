import { openDB, STORES } from "./db-core.js";
import { InvoiceDtoInfo, FinalizeResult, InvoiceEntity } from "./dtos.js";


export class InvoiceRepository {

    async createInvoice(invoiceDtoInfo: InvoiceDtoInfo, year: number, isPaid: boolean, isImported: boolean, finalizeResult: FinalizeResult | undefined): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, 'readwrite');
        const store = transaction.objectStore(STORES.invoices);
        const invoice: Omit<InvoiceEntity, "id"> = {
            info: invoiceDtoInfo,
            year: year,
            isPaid: isPaid,
            isImported: isImported,
            isDeleted: false,
            finalizeResult: finalizeResult,
        };

        return new Promise((resolve, reject) => {
            const req = store.add(invoice);
            req.onsuccess = () => resolve(req.result as number);
            req.onerror = () => reject(req.error);
        });
    }

    async updateInvoice(party: InvoiceEntity): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const req = store.put(party);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async deleteInvoice(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const req = store.delete(id);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async softDeleteInvoice(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        const req = store.get(id);
        return new Promise((resolve, reject) => {
            req.onsuccess = () => {
                const entity = req.result as InvoiceEntity | undefined;
                if (entity) {
                    entity.isDeleted = true;
                    store.put(entity).onsuccess = () => resolve();
                } else {
                    reject(new Error("Entity not found"));
                }
            };
            req.onerror = () => reject(req.error);
        });
    }

    async getAllInvoices(): Promise<InvoiceEntity[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result as InvoiceEntity[]);
            req.onerror = () => reject(req.error);
        });
    }

    async clear(): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readwrite");
        const store = transaction.objectStore(STORES.invoices);
        return new Promise((resolve, reject) => {
            const request = store.clear();
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }
}