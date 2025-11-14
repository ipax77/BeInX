import { openDB, STORES } from "./db-core";
import { DraftRepository } from "./draft-repository";
import { DocumentReferenceAnnotationDto, IDraft, IPartyBaseDto, PartyEntity } from "./dtos";

export class PartyRepository {
    private drafts = new DraftRepository();

    async createParty(party: IPartyBaseDto, isSeller: boolean): Promise<number> {
        const db = await openDB();
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        const transaction = db.transaction(storeName, "readwrite");
        const store = transaction.objectStore(storeName);

        const now = new Date().toISOString();

        const partyEntity: Omit<PartyEntity, "id"> = {
            party,
            createdAt: now,
            updatedAt: now,
        };

        return new Promise((resolve, reject) => {
            const req = store.add(partyEntity);
            req.onsuccess = () => resolve(req.result as number);
            req.onerror = () => reject(req.error);
        });
    }

    async updateParty(id: number, party: IPartyBaseDto, isSeller: boolean): Promise<void> {
        const db = await openDB();
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        const transaction = db.transaction(storeName, "readwrite");
        const store = transaction.objectStore(storeName);

        const existingReq = store.get(id);

        return new Promise((resolve, reject) => {
            existingReq.onsuccess = () => {
                const existing = existingReq.result as PartyEntity | undefined;
                if (!existing) {
                    reject(new Error(`Party with id ${id} not found`));
                    return;
                }

                const updated: PartyEntity = {
                    ...existing,
                    party,
                    updatedAt: new Date().toISOString(),
                };

                const updateReq = store.put(updated);
                updateReq.onsuccess = () => resolve();
                updateReq.onerror = () => reject(updateReq.error);
            };
            existingReq.onerror = () => reject(existingReq.error);
        });
    }

    async deleteParty(id: number, isSeller: boolean): Promise<void> {
        const db = await openDB();
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        const transaction = db.transaction(storeName, "readwrite");
        const store = transaction.objectStore(storeName);

        return new Promise((resolve, reject) => {
            const req = store.delete(id);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async getAllParties(isSeller: boolean): Promise<PartyEntity[]> {
        const db = await openDB();
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        const transaction = db.transaction(storeName, "readonly");
        const store = transaction.objectStore(storeName);

        return new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result as PartyEntity[]);
            req.onerror = () => reject(req.error);
        });
    }

    async setPartyLogo(id: number, logoData: DocumentReferenceAnnotationDto | null, isSeller: boolean): Promise<void> {
        const db = await openDB();
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        const transaction = db.transaction(storeName, "readwrite");
        const store = transaction.objectStore(storeName);
        const existingReq = store.get(id);
        return new Promise((resolve, reject) => {
            existingReq.onsuccess = () => {
                const existing = existingReq.result as PartyEntity | undefined;
                if (!existing) {
                    reject(new Error(`Party with id ${id} not found`));
                    return;
                } else {
                    existing.party.logoReferenceId = logoData?.id;
                    const updated: PartyEntity = {
                        ...existing,
                        logo: logoData ?? undefined,
                        updatedAt: new Date().toISOString(),
                }
                const updateReq = store.put(updated);
                updateReq.onsuccess = () => resolve();
                updateReq.onerror = () => reject(updateReq.error);
            };
        }});
    }

    async clear(): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.sellers, STORES.buyers], "readwrite");
        const sellers = transaction.objectStore(STORES.sellers);
        const buyers = transaction.objectStore(STORES.buyers);

        return new Promise((resolve, reject) => {
            const clearSellers = sellers.clear();
            const clearBuyers = buyers.clear();

            let completed = 0;
            const checkDone = () => {
                if (++completed === 2) resolve();
            };

            clearSellers.onsuccess = checkDone;
            clearBuyers.onsuccess = checkDone;
            clearSellers.onerror = () => reject(clearSellers.error);
            clearBuyers.onerror = () => reject(clearBuyers.error);
        });
    }

    // ---- Temporary Draft handling ----
    async saveTempParty(party: IPartyBaseDto, isSeller: boolean, id?: number) {
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        await this.drafts.saveDraft(storeName, party, id);
    }

    async loadTempParty(isSeller: boolean): Promise<IDraft | null> {
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        return await this.drafts.getDraft(storeName);
    }

    async clearTempParty(isSeller: boolean, entityId?: number) {
        const storeName = isSeller ? STORES.sellers : STORES.buyers;
        await this.drafts.clearDraft(storeName, entityId);
    }
}
