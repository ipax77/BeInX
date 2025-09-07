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
});