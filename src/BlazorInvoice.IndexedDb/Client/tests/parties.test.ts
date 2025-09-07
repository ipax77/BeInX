import { describe, it, expect, afterEach } from 'vitest';
import { IPartyBaseDto } from '../dtos';
import { invoiceRepository, partyRepository, paymentRepository } from '../beinx-db';

const getTestParty = (): IPartyBaseDto => ({
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

export const getTestSeller = (): IPartyBaseDto => ({
    ...getTestParty(),
    name: 'Test Seller Company',
    email: 'seller@company.com',
    buyerReference: 'SELLER-REF-001'
});

export const getTestBuyer = (): IPartyBaseDto => ({
    ...getTestParty(),
    name: 'Test Buyer Company',
    email: 'buyer@company.com',
    buyerReference: 'BUYER-REF-002'
});

describe('PartyRepository CRUD Operations', () => {
    afterEach(async () => {
        await paymentRepository.clear();
        await partyRepository.clear();
        await invoiceRepository.clear();
    });

    it('should create a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, true, undefined);
        expect(id).toBeGreaterThan(0);

        const createdParties = await partyRepository.getAllParties();
        expect(createdParties.shift()?.id).toEqual(id);
    });

    it('should update a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, true, undefined);

        const createdParties = await partyRepository.getAllParties();
        const party = createdParties.find(f => f.id === id);
        expect(party).toBeDefined();
        if (!party) return;
        const newName = "Test Party Update";
        party.party.name = newName;
        await partyRepository.updateParty(party);
        const updatedParties = await partyRepository.getAllParties();
        const updatedParty = updatedParties.find(f => f.id === id);
        expect(updatedParty).toBeDefined();
        expect(updatedParty?.party.name).toEqual(newName);
    });

    it('should delete a party', async () => {
        const testParty = getTestParty();
        const id = await partyRepository.createParty(testParty, true, undefined);

        await partyRepository.deleteParty(id);
        const createdParties = await partyRepository.getAllParties();
        const deletedParty = createdParties.find(f => f.id === id);
        expect(deletedParty).toBeUndefined();
    });
});
