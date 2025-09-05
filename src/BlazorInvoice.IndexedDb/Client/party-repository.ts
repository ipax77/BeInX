import { openDB, STORES } from "./beinx-db.js";
import { PartyListDto, PartyEntity, DocumentReferenceAnnotationDto, SellerAnnotationDto, BuyerAnnotationDto, IPartyBaseDto } from "./dtos.js";

/// Parties

export class PartyRepository {

    /**
     * Gets all parties (sellers or buyers) - let C# handle filtering and pagination
     */
    async getAllParties(isSeller: boolean): Promise<PartyListDto[]> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readonly");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const parties: PartyListDto[] = [];
            const cursor = store.openCursor();

            cursor.onsuccess = (event) => {
                const result = (event.target as IDBRequest).result;
                if (result) {
                    const party = result.value as PartyEntity;
                    if (!party.isDeleted && party.isSeller === isSeller) {
                        parties.push({
                            partyId: party.id!,
                            name: party.name,
                            email: party.email
                        });
                    }
                    result.continue();
                } else {
                    resolve(parties);
                }
            };

            cursor.onerror = () => reject(cursor.error);
        });
    }

    /**
     * Gets party logo as DocumentReferenceAnnotationDto
     */
    async getPartyLogo(partyId: number): Promise<DocumentReferenceAnnotationDto | null> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readonly");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const request = store.get(partyId);

            request.onsuccess = () => {
                const party = request.result as PartyEntity;
                if (!party || !party.logo) {
                    resolve(null);
                    return;
                }

                resolve({
                    id: party.logoReferenceId || crypto.randomUUID(),
                    mimeCode: "image/png",
                    documentDescription: "Party Logo",
                    fileName: `logo_${partyId}.png`,
                    content: party.logo
                });
            };

            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Gets seller data
     */
    async getSeller(partyId: number): Promise<SellerAnnotationDto | null> {
        const party = await this.getPartyById(partyId);
        if (!party) return null;

        return {
            website: party.website,
            logoReferenceId: party.logoReferenceId,
            name: party.name,
            streetName: party.streetName,
            city: party.city,
            postCode: party.postCode,
            countryCode: party.countryCode,
            telefone: party.telefone,
            email: party.email,
            registrationName: party.registrationName,
            taxId: party.taxId,
            companyId: party.companyId,
            buyerReference: party.buyerReference
        };
    }

    /**
     * Gets buyer data
     */
    async getBuyer(partyId: number): Promise<BuyerAnnotationDto | null> {
        const party = await this.getPartyById(partyId);
        if (!party) return null;

        return {
            website: party.website,
            logoReferenceId: party.logoReferenceId,
            name: party.name,
            streetName: party.streetName,
            city: party.city,
            postCode: party.postCode,
            countryCode: party.countryCode,
            telefone: party.telefone,
            email: party.email,
            registrationName: party.registrationName,
            taxId: party.taxId,
            companyId: party.companyId,
            buyerReference: party.buyerReference
        };
    }

    /**
     * Creates a new party
     */
    async createParty(party: IPartyBaseDto, isSeller: boolean): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readwrite");
        const store = transaction.objectStore(STORES.parties);

        const newParty: Omit<PartyEntity, 'id'> = {
            website: party.website,
            logoReferenceId: party.logoReferenceId,
            name: party.name,
            streetName: party.streetName,
            city: party.city,
            postCode: party.postCode,
            countryCode: party.countryCode,
            telefone: party.telefone,
            email: party.email,
            registrationName: party.registrationName,
            taxId: party.taxId,
            companyId: party.companyId,
            buyerReference: party.buyerReference,
            isSeller: isSeller,
            isDeleted: false
        };

        return new Promise((resolve, reject) => {
            const request = store.add(newParty);

            request.onsuccess = () => {
                resolve(request.result as number);
            };

            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Updates an existing party
     */
    async updateParty(partyId: number, party: IPartyBaseDto): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readwrite");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const getRequest = store.get(partyId);

            getRequest.onsuccess = () => {
                const existingParty = getRequest.result as PartyEntity;
                if (!existingParty) {
                    reject(new Error(`Party with ID ${partyId} not found`));
                    return;
                }

                // Update properties
                existingParty.website = party.website;
                existingParty.logoReferenceId = party.logoReferenceId;
                existingParty.name = party.name;
                existingParty.streetName = party.streetName;
                existingParty.city = party.city;
                existingParty.postCode = party.postCode;
                existingParty.countryCode = party.countryCode;
                existingParty.telefone = party.telefone;
                existingParty.email = party.email;
                existingParty.registrationName = party.registrationName;
                existingParty.taxId = party.taxId;
                existingParty.companyId = party.companyId;
                existingParty.buyerReference = party.buyerReference;

                const putRequest = store.put(existingParty);

                putRequest.onsuccess = () => resolve();
                putRequest.onerror = () => reject(putRequest.error);
            };

            getRequest.onerror = () => reject(getRequest.error);
        });
    }

    /**
     * Deletes a party - Let C# determine if soft or hard delete is needed
     */
    async deleteParty(partyId: number, softDelete: boolean = false): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readwrite");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            if (softDelete) {
                // Soft delete - mark as deleted
                const getRequest = store.get(partyId);

                getRequest.onsuccess = () => {
                    const existingParty = getRequest.result as PartyEntity;
                    if (!existingParty) {
                        reject(new Error(`Party with ID ${partyId} not found`));
                        return;
                    }

                    existingParty.isDeleted = true;
                    const putRequest = store.put(existingParty);
                    putRequest.onsuccess = () => resolve();
                    putRequest.onerror = () => reject(putRequest.error);
                };

                getRequest.onerror = () => reject(getRequest.error);
            } else {
                // Hard delete
                const deleteRequest = store.delete(partyId);
                deleteRequest.onsuccess = () => resolve();
                deleteRequest.onerror = () => reject(deleteRequest.error);
            }
        });
    }

    /**
     * Updates party logo
     */
    async updatePartyLogo(partyId: number, logoBase64: string, logoReferenceId?: string): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readwrite");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const getRequest = store.get(partyId);

            getRequest.onsuccess = () => {
                const existingParty = getRequest.result as PartyEntity;
                if (!existingParty) {
                    reject(new Error(`Party with ID ${partyId} not found`));
                    return;
                }

                existingParty.logo = logoBase64;
                existingParty.logoReferenceId = logoReferenceId || crypto.randomUUID();

                const putRequest = store.put(existingParty);
                putRequest.onsuccess = () => resolve();
                putRequest.onerror = () => reject(putRequest.error);
            };

            getRequest.onerror = () => reject(getRequest.error);
        });
    }

    /**
     * Gets all parties referenced by invoices (for determining if soft delete is needed)
     */
    async getReferencedPartyIds(): Promise<Set<number>> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readonly");
        const store = transaction.objectStore(STORES.invoices);

        return new Promise((resolve, reject) => {
            const referencedIds = new Set<number>();
            const cursor = store.openCursor();

            cursor.onsuccess = (event) => {
                const result = (event.target as IDBRequest).result;
                if (result) {
                    const invoice = result.value;
                    if (invoice.sellerPartyId) referencedIds.add(invoice.sellerPartyId);
                    if (invoice.buyerPartyId) referencedIds.add(invoice.buyerPartyId);
                    result.continue();
                } else {
                    resolve(referencedIds);
                }
            };

            cursor.onerror = () => reject(cursor.error);
        });
    }

    // Helper method
    private async getPartyById(partyId: number): Promise<PartyEntity | null> {
        const db = await openDB();
        const transaction = db.transaction([STORES.parties], "readonly");
        const store = transaction.objectStore(STORES.parties);

        return new Promise((resolve, reject) => {
            const request = store.get(partyId);

            request.onsuccess = () => {
                resolve(request.result as PartyEntity || null);
            };

            request.onerror = () => reject(request.error);
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
