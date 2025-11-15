import { openDB, STORES } from "./db-core";
import { DraftRepository } from "./draft-repository";
import { FinalizeResult, IDraft, InvoiceDtoInfo, InvoiceEntity, InvoiceListItem, InvoicesRequest, IPaymentMeansBaseDto, PaymentMeansEntity } from "./dtos";

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
                    payableAmount: dto.payableAmount,
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

    async getFilteredInvoiceList(request: InvoicesRequest): Promise<InvoiceListItem[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const allItems: InvoiceListItem[] = [];
            const cursorReq = store.openCursor();

            cursorReq.onsuccess = () => {
                const cursor = cursorReq.result;
                if (!cursor) {
                    // Apply sorting after collecting all items
                    const sorted = allItems.sort((a, b) => {
                        const key = request.sortBy;
                        const asc = request.sortAsc ? 1 : -1;
                        const aVal = a[key];
                        const bVal = b[key];

                        if (aVal == null && bVal == null) return 0;
                        if (aVal == null) return -1 * asc;
                        if (bVal == null) return 1 * asc;

                        // String comparison
                        if (typeof aVal === "string" && typeof bVal === "string") {
                            return aVal.localeCompare(bVal) * asc;
                        }

                        // Boolean or number comparison
                        if (aVal > bVal) return 1 * asc;
                        if (aVal < bVal) return -1 * asc;
                        return 0;
                    });

                    // Apply pagination
                    const page = request.page ?? 0;
                    const pageSize = request.pageSize ?? Number.MAX_SAFE_INTEGER;
                    const startIndex = page * pageSize;
                    const paginated = sorted.slice(startIndex, startIndex + pageSize);

                    resolve(paginated);
                    return;
                }

                const value = cursor.value as InvoiceEntity;
                if (this.matchesFilters(value, request)) {
                    const dto = value.info.invoiceDto;
                    allItems.push({
                        id: value.id,
                        invoiceId: dto.id,
                        issueDate: dto.issueDate,
                        sellerName: dto.sellerParty?.name || "",
                        buyerName: dto.buyerParty?.name || "",
                        isPaid: value.isPaid,
                        year: value.year,
                        payableAmount: dto.payableAmount,
                    });
                }

                cursor.continue();
            };

            cursorReq.onerror = () => reject(cursorReq.error);
        });
    }

    async getFilteredInvoiceCount(request: InvoicesRequest): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.invoices, "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            let count = 0;
            const cursorReq = store.openCursor();

            cursorReq.onsuccess = () => {
                const cursor = cursorReq.result;
                if (!cursor) {
                    resolve(count);
                    return;
                }

                const value = cursor.value as InvoiceEntity;

                if (this.matchesFilters(value, request)) {
                    count++;
                }

                cursor.continue();
            };

            cursorReq.onerror = () => reject(cursorReq.error);
        });
    }

    private matchesFilters(value: InvoiceEntity, request: InvoicesRequest): boolean {
        const dto = value.info.invoiceDto;

        // Filter by year
        if (request.year !== null && value.year !== request.year) {
            return false;
        }

        // Filter by isPaid
        if (request.isPaid !== null && value.isPaid !== request.isPaid) {
            return false;
        }

        // Filter by search
        if (request.search) {
            const searchLower = request.search.toLowerCase();
            const sellerName = (dto.sellerParty?.name || "").toLowerCase();
            const buyerName = (dto.buyerParty?.name || "").toLowerCase();
            const invoiceId = dto.id.toLowerCase();
            
            if (!invoiceId.includes(searchLower) && !sellerName.includes(searchLower) && !buyerName.includes(searchLower)) {
                return false;
            }
        }

        return true;
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