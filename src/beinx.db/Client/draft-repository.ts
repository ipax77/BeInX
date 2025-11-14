import { openDB, STORES } from "./db-core";
import { InvoiceDtoInfo, IPartyBaseDto, IPaymentMeansBaseDto } from "./dtos";

export interface IDraft {
  id: string;
  entityType: string;
  entityId?: number;
  data: IPaymentMeansBaseDto | IPartyBaseDto;
  updatedAt: string;
}

export class DraftRepository {
    async saveDraft(entityType: string, data: IPaymentMeansBaseDto | IPartyBaseDto | InvoiceDtoInfo, entityId?: number): Promise<void> {
        const db = await openDB();
        return new Promise<void>((resolve, reject) => {
            const tx = db.transaction(STORES.drafts, 'readwrite');
            const store = tx.objectStore(STORES.drafts);

            const id = entityId ? `${entityType}-${entityId}` : `${entityType}-draft`;

            const draft = {
                id,
                entityType,
                entityId: entityId ?? null,
                data,
                updatedAt: new Date().toISOString(),
            };

            store.put(draft);
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
        });
    }

    async getDraft(entityType: string): Promise<IDraft | null> {
        const db = await openDB();
        return new Promise<IDraft | null>((resolve, reject) => {
            const tx = db.transaction(STORES.drafts, 'readonly');
            const store = tx.objectStore(STORES.drafts);

            // There should be at most one draft per entityType
            const request = store.getAll();
            request.onsuccess = () => {
                const drafts = request.result.filter(x => x.entityType === entityType);
                resolve(drafts.length ? drafts[0] : null);
            };
            request.onerror = () => reject(request.error);
        });
    }

    async clearDraft(entityType: string, entityId?: number): Promise<void> {
        const db = await openDB();
        return new Promise<void>((resolve, reject) => {
            const tx = db.transaction(STORES.drafts, 'readwrite');
            const store = tx.objectStore(STORES.drafts);
            const id = entityId ? `${entityType}-${entityId}` : `${entityType}-draft`;
            store.delete(id);
            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
        });
    }

    async getAllDrafts() {
        const db = await openDB();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORES.drafts, "readonly");
            const store = tx.objectStore(STORES.drafts);
            const req = store.getAll();

            req.onsuccess = () => {
                let results = req.result;
                resolve(results);
            };
            req.onerror = () => reject(req.error);
        });
    }
}
