import { openDB, STORES } from "./db-core";
import { DraftRepository } from "./draft-repository";
import { FinalizeResult, IDraft, InvoiceDtoInfo, InvoiceEntity, InvoiceListItem, IPaymentMeansBaseDto, PaymentMeansEntity } from "./dtos";

export class InvoiceRepository {
    private drafts = new DraftRepository();

    async createInvoice(invoiceInfo: InvoiceDtoInfo, isImported: boolean): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        const entity: Omit<InvoiceEntity, "id"> = {
            info: invoiceInfo,
            year: new Date(invoiceInfo.invoiceDto.issueDate).getFullYear(),
            isPaid: false,
            isImported,
            updatedAt: new Date().toISOString()
        };

        return new Promise((resolve, reject) => {
            const req = store.add(entity);
            req.onsuccess = () => resolve(req.result as number);
            req.onerror = () => reject(req.error);
        });
    }

    async updateInvoice(id: number, updatedInfo: Partial<InvoiceDtoInfo>): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        const existingReq = store.get(id);

        return new Promise((resolve, reject) => {
            existingReq.onsuccess = () => {
                const existing = existingReq.result as InvoiceEntity | undefined;
                if (!existing) {
                    reject(new Error(`Invoice with id ${id} not found`));
                    return;
                }

                const updated: InvoiceEntity = {
                    ...existing,
                    info: { ...existing.info, ...updatedInfo },
                    updatedAt: new Date().toISOString(),
                };

                const updateReq = store.put(updated);
                updateReq.onsuccess = () => resolve();
                updateReq.onerror = () => reject(updateReq.error);
            };
            existingReq.onerror = () => reject(existingReq.error);
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

    async getById(id: number): Promise<InvoiceEntity | null> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const req = store.get(id);
            req.onsuccess = () => resolve(req.result as InvoiceEntity || null);
            req.onerror = () => reject(req.error);
        });
    }

    async findByYear(year: number): Promise<InvoiceEntity[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);
        const index = store.index("year");

        return new Promise((resolve, reject) => {
            const req = index.getAll(year);
            req.onsuccess = () => resolve(req.result as InvoiceEntity[]);
            req.onerror = () => reject(req.error);
        });
    }

    async setPaid(id: number, isPaid: boolean): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        const existingReq = store.get(id);

        return new Promise((resolve, reject) => {
            existingReq.onsuccess = () => {
                const existing = existingReq.result as InvoiceEntity | undefined;
                if (!existing) {
                    reject(new Error(`Invoice with id ${id} not found`));
                    return;
                }

                const updated: InvoiceEntity = {
                    ...existing,
                    isPaid: isPaid,
                    updatedAt: new Date().toISOString(),
                };

                const updateReq = store.put(updated);
                updateReq.onsuccess = () => resolve();
                updateReq.onerror = () => reject(updateReq.error);
            };
            existingReq.onerror = () => reject(existingReq.error);
        });
    }

    async finalizeInvoice(id: number, finalizeResult: FinalizeResult): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readwrite");
        const store = transaction.objectStore(STORES.invoices);

        const existingReq = store.get(id);

        return new Promise((resolve, reject) => {
            existingReq.onsuccess = () => {
                const existing = existingReq.result as InvoiceEntity | undefined;
                if (!existing) {
                    reject(new Error(`Invoice with id ${id} not found`));
                    return;
                }

                const updated: InvoiceEntity = {
                    ...existing,
                    finalizeResult,
                    updatedAt: new Date().toISOString(),
                };

                const updateReq = store.put(updated);
                updateReq.onsuccess = () => resolve();
                updateReq.onerror = () => reject(updateReq.error);
            };

            existingReq.onerror = () => reject(existingReq.error);
        });
    }

    async getInvoiceCount(): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const req = store.count();
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    async getInvoiceList(limit = 50, offset = 0): Promise<InvoiceListItem[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const result: InvoiceListItem[] = [];
            let skipped = 0;

            const cursorReq = store.openCursor();

            cursorReq.onsuccess = () => {
                const cursor = cursorReq.result;
                if (!cursor) {
                    resolve(result);
                    return;
                }

                if (skipped < offset) {
                    skipped++;
                    cursor.continue();
                    return;
                }

                const value = cursor.value as InvoiceEntity;
                const dto = value.info.invoiceDto;

                result.push({
                    id: value.id,
                    invoiceId: dto.id,
                    issueDate: dto.issueDate,
                    sellerName: dto.sellerParty?.name || "",
                    buyerName: dto.buyerParty?.name || "",
                    isPaid: value.isPaid,
                    year: value.year,
                });

                if (result.length >= limit) {
                    resolve(result);
                    return;
                }

                cursor.continue();
            };

            cursorReq.onerror = () => reject(cursorReq.error);
        });
    }

    // ---- Draft helpers ----

    async saveTempInvoice(invoice: InvoiceDtoInfo, id?: number) {
        await this.drafts.saveDraft(STORES.invoices, invoice, id);
    }

    async loadTempInvoice(): Promise<IDraft | null> {
        return await this.drafts.getDraft(STORES.invoices);
    }

    async clearTempInvoice(entityId?: number) {
        await this.drafts.clearDraft(STORES.invoices, entityId);
    }
}