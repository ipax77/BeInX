import { describe, it, expect, afterEach } from 'vitest';
import { IPartyBaseDto } from '../dtos';
import { partyRepository } from '../beinx-db';

export const getTestParty = (): IPartyBaseDto => ({
    website: 'https://testcompany.com',
    logoReferenceId: undefined,
    name: 'Test Company Ltd.',
    streetName: 'Test Street 123',
    city: 'Test City',
    postCode: '12345',
    countryCode: 'DE',
    telefone: '+49123456789',
    email: 'test@company.com',
    registrationName: 'Test Company Limited',
    taxId: 'DE123456789',
    companyId: 'HRB12345',
    buyerReference: 'BUYER-REF-001'
});

describe('PartyRepository CRUD Operations', () => {
    afterEach(async () => {
        await partyRepository.clear();
    });

    it('should create a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, true);
        expect(id).toBeGreaterThan(0);

        const createdParties = await partyRepository.getAllParties(true);
        expect(createdParties.length).toBe(1);
        expect(createdParties[0].id).toEqual(id);
        expect(createdParties[0].party).toEqual(testParty);
    });

    it('should update a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, true);

        const updatedParty = { ...testParty, name: 'Updated Seller Name' };
        await partyRepository.updateParty(id, updatedParty, true);

        const parties = await partyRepository.getAllParties(true);
        expect(parties[0].party.name).toEqual('Updated Seller Name');
    });

    it('should delete a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, false);

        await partyRepository.deleteParty(id, false);
        const buyers = await partyRepository.getAllParties(false);

        expect(buyers.length).toBe(0);
    });

    it('should get all parties for both buyers and sellers', async () => {
        const seller1 = getTestParty();
        const buyer1 = getTestParty();

        await partyRepository.createParty(seller1, true);
        await partyRepository.createParty(buyer1, false);

        const sellers = await partyRepository.getAllParties(true);
        const buyers = await partyRepository.getAllParties(false);

        expect(sellers.length).toBe(1);
        expect(buyers.length).toBe(1);
    });

    it('should clear all parties', async () => {
        await partyRepository.createParty(getTestParty(), true);
        await partyRepository.createParty(getTestParty(), false);

        await partyRepository.clear();

        const sellers = await partyRepository.getAllParties(true);
        const buyers = await partyRepository.getAllParties(false);

        expect(sellers.length).toBe(0);
        expect(buyers.length).toBe(0);
    });

    it('should handle temporary draft save/load/clear correctly', async () => {
        const tempParty = getTestParty();

        await partyRepository.saveTempParty(tempParty, true);
        const draft = await partyRepository.loadTempParty(true);

        expect(draft).not.toBeNull();
        expect(draft?.data).toEqual(tempParty);

        await partyRepository.clearTempParty(true);
        const cleared = await partyRepository.loadTempParty(true);
        expect(cleared).toBeNull();
    });
});