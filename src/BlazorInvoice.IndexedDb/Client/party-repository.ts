import { openDB, STORES } from "./beinx-db";
import { IPartyBaseDto, DocumentReferenceAnnotationDto, PartyEntity } from "./dtos";


export class PartyRepository {

    async createParty(partyDto: IPartyBaseDto, isSeller: boolean, logo: DocumentReferenceAnnotationDto | undefined): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.parties, 'readwrite');
        const store = transaction.objectStore(STORES.parties);
        const party: Omit<PartyEntity, "id"> = {
            party: partyDto,
            isSeller: isSeller,
            logo: logo,
            isDeleted: false
        };

        return new Promise((resolve, reject) => {
            const req = store.add(party);
            req.onsuccess = () => resolve(req.result as number);
            req.onerror = () => reject(req.error);
        });
    }

    async updateParty(party: PartyEntity): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.parties, "readwrite");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const req = store.put(party);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async deleteParty(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.parties, "readwrite");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const req = store.delete(id);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async softDeleteParty(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.parties, "readwrite");
        const store = transaction.objectStore(STORES.parties);

        const req = store.get(id);
        return new Promise((resolve, reject) => {
            req.onsuccess = () => {
                const entity = req.result as PartyEntity | undefined;
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

    async getAllParties(): Promise<PartyEntity[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.parties, "readonly");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result as PartyEntity[]);
            req.onerror = () => reject(req.error);
        });
    }

    async clear(): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readwrite");
        const store = transaction.objectStore(STORES.parties);
        return new Promise((resolve, reject) => {
            const request = store.clear();
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }
}
