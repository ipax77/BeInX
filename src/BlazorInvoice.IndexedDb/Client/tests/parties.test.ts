import { describe, it, expect } from 'vitest';
import { IPartyBaseDto } from '../dtos';
import { partyRepository } from '../beinx-db';

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

const getTestSeller = (): IPartyBaseDto => ({
    ...getTestParty(),
    name: 'Test Seller Company',
    email: 'seller@company.com',
    buyerReference: 'SELLER-REF-001'
});

const getTestBuyer = (): IPartyBaseDto => ({
    ...getTestParty(),
    name: 'Test Buyer Company',
    email: 'buyer@company.com',
    buyerReference: 'BUYER-REF-002'
});

describe('PartyRepository CRUD Operations', () => {
    describe('Seller Operations', () => {
        it('should create a seller', async () => {
            const testSeller = getTestSeller();
            const id = await partyRepository.createParty(testSeller, true);
            expect(id).toBeGreaterThan(0);

            const createdSeller = await partyRepository.getSeller(id);
            expect(createdSeller).toEqual({
                ...testSeller,
                logoReferenceId: undefined
            });
        });

        it('should update a seller', async () => {
            const testSeller = getTestSeller();
            const id = await partyRepository.createParty(testSeller, true);

            const updatedSellerDto: IPartyBaseDto = {
                ...testSeller,
                name: 'Updated Seller Company',
                email: 'updated@seller.com'
            };

            await partyRepository.updateParty(id, updatedSellerDto);

            const updatedSeller = await partyRepository.getSeller(id);
            expect(updatedSeller?.name).toBe('Updated Seller Company');
            expect(updatedSeller?.email).toBe('updated@seller.com');
        });

        it('should get all sellers', async () => {
            const testSeller1 = { ...getTestSeller(), name: 'Seller One', email: 'seller1@test.com' };
            const testSeller2 = { ...getTestSeller(), name: 'Seller Two', email: 'seller2@test.com' };

            const id1 = await partyRepository.createParty(testSeller1, true);
            const id2 = await partyRepository.createParty(testSeller2, true);

            const sellers = await partyRepository.getAllParties(true);
            
            expect(sellers.length).toBeGreaterThanOrEqual(2);
            
            const createdSellers = sellers.filter(s => s.partyId === id1 || s.partyId === id2);
            expect(createdSellers).toHaveLength(2);
            
            expect(createdSellers.find(s => s.partyId === id1)).toEqual({
                partyId: id1,
                name: 'Seller One',
                email: 'seller1@test.com'
            });
        });

        it('should hard delete a seller', async () => {
            const testSeller = getTestSeller();
            const id = await partyRepository.createParty(testSeller, true);

            await partyRepository.deleteParty(id, false); // Hard delete

            const deletedSeller = await partyRepository.getSeller(id);
            expect(deletedSeller).toBeNull();
        });

        it('should soft delete a seller', async () => {
            const testSeller = getTestSeller();
            const id = await partyRepository.createParty(testSeller, true);

            await partyRepository.deleteParty(id, true); // Soft delete

            // Should not appear in getAllParties (filters out deleted)
            const sellers = await partyRepository.getAllParties(true);
            const deletedSeller = sellers.find(s => s.partyId === id);
            expect(deletedSeller).toBeUndefined();
        });
    });

    describe('Buyer Operations', () => {
        it('should create a buyer', async () => {
            const testBuyer = getTestBuyer();
            const id = await partyRepository.createParty(testBuyer, false);
            expect(id).toBeGreaterThan(0);

            const createdBuyer = await partyRepository.getBuyer(id);
            expect(createdBuyer).toEqual({
                ...testBuyer,
                logoReferenceId: undefined
            });
        });

        it('should update a buyer', async () => {
            const testBuyer = getTestBuyer();
            const id = await partyRepository.createParty(testBuyer, false);

            const updatedBuyerDto: IPartyBaseDto = {
                ...testBuyer,
                name: 'Updated Buyer Company',
                city: 'Updated City'
            };

            await partyRepository.updateParty(id, updatedBuyerDto);

            const updatedBuyer = await partyRepository.getBuyer(id);
            expect(updatedBuyer?.name).toBe('Updated Buyer Company');
            expect(updatedBuyer?.city).toBe('Updated City');
        });

        it('should get all buyers', async () => {
            const testBuyer1 = { ...getTestBuyer(), name: 'Buyer One', email: 'buyer1@test.com' };
            const testBuyer2 = { ...getTestBuyer(), name: 'Buyer Two', email: 'buyer2@test.com' };

            const id1 = await partyRepository.createParty(testBuyer1, false);
            const id2 = await partyRepository.createParty(testBuyer2, false);

            const buyers = await partyRepository.getAllParties(false);
            
            expect(buyers.length).toBeGreaterThanOrEqual(2);
            
            const createdBuyers = buyers.filter(b => b.partyId === id1 || b.partyId === id2);
            expect(createdBuyers).toHaveLength(2);
            
            expect(createdBuyers.find(b => b.partyId === id2)).toEqual({
                partyId: id2,
                name: 'Buyer Two',
                email: 'buyer2@test.com'
            });
        });

        it('should delete a buyer', async () => {
            const testBuyer = getTestBuyer();
            const id = await partyRepository.createParty(testBuyer, false);

            await partyRepository.deleteParty(id, false);

            const deletedBuyer = await partyRepository.getBuyer(id);
            expect(deletedBuyer).toBeNull();
        });
    });

    describe('Logo Operations', () => {
        it('should update party logo', async () => {
            const testParty = getTestParty();
            const id = await partyRepository.createParty(testParty, true);

            const logoBase64 = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==';
            const logoReferenceId = 'test-logo-ref-123';

            await partyRepository.updatePartyLogo(id, logoBase64, logoReferenceId);

            const logo = await partyRepository.getPartyLogo(id);
            expect(logo).not.toBeNull();
            expect(logo?.id).toBe(logoReferenceId);
            expect(logo?.mimeCode).toBe('image/png');
            expect(logo?.documentDescription).toBe('Party Logo');
            expect(logo?.fileName).toBe(`logo_${id}.png`);
            expect(logo?.content).toBe(logoBase64);
        });

        it('should return null for party without logo', async () => {
            const testParty = getTestParty();
            const id = await partyRepository.createParty(testParty, true);

            const logo = await partyRepository.getPartyLogo(id);
            expect(logo).toBeNull();
        });

        it('should generate random UUID for logo reference when not provided', async () => {
            const testParty = getTestParty();
            const id = await partyRepository.createParty(testParty, true);

            const logoBase64 = 'data:image/png;base64,test';
            await partyRepository.updatePartyLogo(id, logoBase64); // No logoReferenceId provided

            const logo = await partyRepository.getPartyLogo(id);
            expect(logo).not.toBeNull();
            expect(logo?.id).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i); // UUID format
        });
    });

    describe('Referenced Parties', () => {
        it('should get referenced party IDs', async () => {
            // This test would require mock invoice data or a more complex setup
            // For now, test that the method returns a Set
            const referencedIds = await partyRepository.getReferencedPartyIds();
            expect(referencedIds).toBeInstanceOf(Set);
        });
    });

    describe('Error Handling', () => {
        it('should handle getting non-existent seller', async () => {
            const nonExistentId = 99999;
            const seller = await partyRepository.getSeller(nonExistentId);
            expect(seller).toBeNull();
        });

        it('should handle getting non-existent buyer', async () => {
            const nonExistentId = 99999;
            const buyer = await partyRepository.getBuyer(nonExistentId);
            expect(buyer).toBeNull();
        });

        it('should throw error when updating non-existent party', async () => {
            const nonExistentId = 99999;
            const testParty = getTestParty();

            await expect(partyRepository.updateParty(nonExistentId, testParty))
                .rejects.toThrow('Party with ID 99999 not found');
        });

        it('should throw error when updating logo for non-existent party', async () => {
            const nonExistentId = 99999;
            const logoBase64 = 'data:image/png;base64,test';

            await expect(partyRepository.updatePartyLogo(nonExistentId, logoBase64))
                .rejects.toThrow('Party with ID 99999 not found');
        });
    });

    describe('Data Integrity', () => {
        it('should maintain isSeller flag correctly', async () => {
            const testParty = getTestParty();
            
            const sellerId = await partyRepository.createParty(testParty, true);
            const buyerId = await partyRepository.createParty(testParty, false);

            const sellers = await partyRepository.getAllParties(true);
            const buyers = await partyRepository.getAllParties(false);

            expect(sellers.find(s => s.partyId === sellerId)).toBeDefined();
            expect(sellers.find(s => s.partyId === buyerId)).toBeUndefined();

            expect(buyers.find(b => b.partyId === buyerId)).toBeDefined();
            expect(buyers.find(b => b.partyId === sellerId)).toBeUndefined();
        });

        it('should not return soft deleted parties in getAllParties', async () => {
            const testParty = getTestParty();
            const id = await partyRepository.createParty(testParty, true);

            let sellers = await partyRepository.getAllParties(true);
            expect(sellers.find(s => s.partyId === id)).toBeDefined();

            await partyRepository.deleteParty(id, true); // Soft delete

            sellers = await partyRepository.getAllParties(true);
            expect(sellers.find(s => s.partyId === id)).toBeUndefined();
        });
    });
});