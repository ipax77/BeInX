import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { InvoiceEntity, InvoiceLineEntity, DocumentReferenceEntity, IPaymentMeansBaseDto } from '../dtos';
import { createPaymentMeans, invoiceRepository, partyRepository } from '../beinx-db';

// Helper functions for test data
const getTestInvoiceLine = (): InvoiceLineEntity => ({
    id: 'LINE-001',
    note: 'Test service',
    quantity: 2,
    quantityCode: 'HUR',
    unitPrice: 50.0,
    startDate: '2025-01-01T00:00:00.000Z',
    endDate: '2025-01-31T23:59:59.999Z',
    description: 'Consulting services for January',
    name: 'Monthly Consulting'
});

const getTestDocumentReference = (): DocumentReferenceEntity => ({
    id: 'DOC-001',
    documentDescription: 'Test Document',
    mimeCode: 'application/pdf',
    fileName: 'test-doc.pdf',
    content: 'data:application/pdf;base64,JVBERi0xLjQKJcfsj6IKCgoxIDAgb2JqCjw8Ci9UeXBlIC9DYXRhbG9nCi9QYWdlcyAyIDAgUgo+PgplbmRvYmoK'
});

const getTestInvoice = (sellerId: number = 1, buyerId: number = 2, paymentId: number = 1): Omit<InvoiceEntity, 'id'> => ({
    globalTaxCategory: 'S',
    globalTaxScheme: 'VAT',
    globalTax: 19.0,
    invoiceId: 'INV-2025-001',
    issueDate: '2025-01-15T00:00:00.000Z',
    dueDate: '2025-02-14T00:00:00.000Z',
    note: 'Test invoice for consulting services',
    invoiceTypeCode: '380',
    documentCurrencyCode: 'EUR',
    paymentTermsNote: 'Payment due within 30 days',
    payableAmount: 119.0,
    sellerPartyId: sellerId,
    buyerPartyId: buyerId,
    paymentMeansId: paymentId,
    invoiceLines: [getTestInvoiceLine()],
    additionalDocumentReferences: [getTestDocumentReference()],
    isPaid: false,
    isDeleted: false
});

// Helper to create test parties and payment
const createTestRelatedData = async () => {
    const sellerId = await partyRepository.createParty({
        name: 'Test Seller Company',
        email: 'seller@test.com',
        city: 'Test City',
        postCode: '12345',
        countryCode: 'DE',
        telefone: '+49123456789',
        registrationName: 'Test Seller Ltd',
        taxId: 'DE123456789',
        buyerReference: 'SELLER-REF'
    }, true);

    const buyerId = await partyRepository.createParty({
        name: 'Test Buyer Company',
        email: 'buyer@test.com',
        city: 'Buyer City',
        postCode: '54321',
        countryCode: 'DE',
        telefone: '+49987654321',
        registrationName: 'Test Buyer Ltd',
        taxId: 'DE987654321',
        buyerReference: 'BUYER-REF'
    }, false);

    const paymentId = await createPaymentMeans({
        name: 'Test Payment',
        iban: 'DE12345678901234567890',
        bic: 'TESTDEFFXXX',
        paymentMeansTypeCode: '30',
    });

    return { sellerId, buyerId, paymentId };
};

describe('InvoiceRepository CRUD Operations', () => {
    let testData: { sellerId: number; buyerId: number; paymentId: number; };

    beforeEach(async () => {
        testData = await createTestRelatedData();
    });

    afterEach(async () => {
        await invoiceRepository.clear();
        await partyRepository.clear();
    });

    describe('Basic CRUD Operations', () => {
        it('should create an invoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);

            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);
            expect(id).toBeGreaterThan(0);

            const createdInvoice = await invoiceRepository.getInvoice(id);
            expect(createdInvoice).not.toBeNull();
            expect(createdInvoice?.invoiceId).toBe('INV-2025-001');
            expect(createdInvoice?.sellerPartyId).toBe(sellerId);
            expect(createdInvoice?.buyerPartyId).toBe(buyerId);
            expect(createdInvoice?.paymentMeansId).toBe(paymentId);
            expect(createdInvoice?.isPaid).toBe(false);
            expect(createdInvoice?.isDeleted).toBe(false);
        });

        it('should get an invoice by ID', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const retrievedInvoice = await invoiceRepository.getInvoice(id);
            expect(retrievedInvoice).not.toBeNull();
            expect(retrievedInvoice?.id).toBe(id);
            expect(retrievedInvoice?.invoiceId).toBe('INV-2025-001');
            expect(retrievedInvoice?.globalTax).toBe(19.0);
            expect(retrievedInvoice?.payableAmount).toBe(119.0);
        });

        it('should update an invoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const updateData = {
                invoiceId: 'INV-2025-001-UPDATED',
                payableAmount: 150.0,
                note: 'Updated test invoice'
            };

            await invoiceRepository.updateInvoice(id, updateData);

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            expect(updatedInvoice?.invoiceId).toBe('INV-2025-001-UPDATED');
            expect(updatedInvoice?.payableAmount).toBe(150.0);
            expect(updatedInvoice?.note).toBe('Updated test invoice');
            // Ensure embedded data is preserved
            expect(updatedInvoice?.invoiceLines).toHaveLength(1);
            expect(updatedInvoice?.additionalDocumentReferences).toHaveLength(1);
        });

        it('should delete an invoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            await invoiceRepository.deleteInvoice(id);

            const deletedInvoice = await invoiceRepository.getInvoice(id);
            expect(deletedInvoice).toBeNull();
        });

        it('should get all invoices', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice1 = { ...getTestInvoice(sellerId, buyerId, paymentId), invoiceId: 'INV-001' };
            const testInvoice2 = { ...getTestInvoice(sellerId, buyerId, paymentId), invoiceId: 'INV-002' };

            await invoiceRepository.createInvoice(testInvoice1, sellerId, buyerId, paymentId);
            await invoiceRepository.createInvoice(testInvoice2, sellerId, buyerId, paymentId);

            const allInvoices = await invoiceRepository.getAllInvoices();

            expect(allInvoices.length).toBe(2);
        });
    });

    describe('Embedded Data Operations', () => {
        it('should preserve invoice lines when creating invoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const additionalLine = {
                ...getTestInvoiceLine(),
                id: 'LINE-002',
                name: 'Additional Service',
                unitPrice: 75.0
            };
            testInvoice.invoiceLines.push(additionalLine);

            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);
            const createdInvoice = await invoiceRepository.getInvoice(id);

            expect(createdInvoice?.invoiceLines).toHaveLength(2);
            expect(createdInvoice?.invoiceLines[0].name).toBe('Monthly Consulting');
            expect(createdInvoice?.invoiceLines[1].name).toBe('Additional Service');
            expect(createdInvoice?.invoiceLines[1].unitPrice).toBe(75.0);
        });

        it('should preserve document references when creating invoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const additionalDoc = {
                ...getTestDocumentReference(),
                id: 'DOC-002',
                documentDescription: 'Additional Document',
                fileName: 'additional.pdf'
            };
            testInvoice.additionalDocumentReferences.push(additionalDoc);

            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);
            const createdInvoice = await invoiceRepository.getInvoice(id);

            expect(createdInvoice?.additionalDocumentReferences).toHaveLength(2);
            expect(createdInvoice?.additionalDocumentReferences[0].documentDescription).toBe('Test Document');
            expect(createdInvoice?.additionalDocumentReferences[1].documentDescription).toBe('Additional Document');
            expect(createdInvoice?.additionalDocumentReferences[1].fileName).toBe('additional.pdf');
        });

        it('should update embedded invoice lines', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const updatedLines = [
                { ...getTestInvoiceLine(), unitPrice: 60.0 },
                { ...getTestInvoiceLine(), id: 'LINE-002', name: 'New Service', unitPrice: 40.0 }
            ];

            await invoiceRepository.updateInvoice(id, { invoiceLines: updatedLines });

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            expect(updatedInvoice?.invoiceLines).toHaveLength(2);
            expect(updatedInvoice?.invoiceLines[0].unitPrice).toBe(60.0);
            expect(updatedInvoice?.invoiceLines[1].name).toBe('New Service');
        });
    });

    describe('Payment Status Operations', () => {
        it('should set invoice as paid', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            await invoiceRepository.setIsPaid(id, true);

            const paidInvoice = await invoiceRepository.getInvoice(id);
            expect(paidInvoice?.isPaid).toBe(true);
        });

        it('should set invoice as unpaid', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = { ...getTestInvoice(sellerId, buyerId, paymentId), isPaid: true };
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            await invoiceRepository.setIsPaid(id, false);

            const unpaidInvoice = await invoiceRepository.getInvoice(id);
            expect(unpaidInvoice?.isPaid).toBe(false);
        });
    });

    describe('XML Finalization Operations', () => {
        it('should finalize invoice with XML data', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const xmlData = new TextEncoder().encode('<xml>test invoice</xml>').buffer;
            const testHash = 'test-hash-123';
            const totalAmount = 100.0;

            const result = await invoiceRepository.finalizeInvoice(id, xmlData, testHash, totalAmount);

            expect(result.xmlInvoiceSha1Hash).toBe(testHash);
            expect(result.xmlInvoiceBlob).toEqual(xmlData);
            expect(result.xmlInvoiceCreated).toBeTruthy();

            const finalizedInvoice = await invoiceRepository.getInvoice(id);
            expect(finalizedInvoice?.xmlInvoiceSha1Hash).toBe(testHash);
            expect(finalizedInvoice?.totalAmountWithoutVat).toBe(totalAmount);
            expect(finalizedInvoice?.xmlInvoiceBlob).toEqual(xmlData);
        });

        it('should get XML blob', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const xmlData = new TextEncoder().encode('<xml>test invoice</xml>').buffer;
            await invoiceRepository.finalizeInvoice(id, xmlData, 'hash', 100.0);

            const retrievedBlob = await invoiceRepository.getXmlBlob(id);
            expect(retrievedBlob).toEqual(xmlData);
        });

        it('should return null for XML blob when not finalized', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const retrievedBlob = await invoiceRepository.getXmlBlob(id);
            expect(retrievedBlob).toBeNull();
        });

        it('should validate XML invoice hash', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const xmlData = new TextEncoder().encode('<xml>test invoice</xml>').buffer;

            // Compute actual SHA1 hash
            const hashBuffer = await crypto.subtle.digest('SHA-1', xmlData);
            const hashArray = new Uint8Array(hashBuffer);
            const actualHash = btoa(String.fromCharCode(...hashArray));

            await invoiceRepository.finalizeInvoice(id, xmlData, actualHash, 100.0);

            const isValid = await invoiceRepository.validateXmlInvoiceHash(id);
            expect(isValid).toBe(true);
        });

        it('should return false for invalid hash', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const xmlData = new TextEncoder().encode('<xml>test invoice</xml>').buffer;
            const wrongHash = 'wrong-hash';

            await invoiceRepository.finalizeInvoice(id, xmlData, wrongHash, 100.0);

            const isValid = await invoiceRepository.validateXmlInvoiceHash(id);
            expect(isValid).toBe(false);
        });
    });

    describe('Document Reference Operations', () => {
        it('should add seller logo document reference', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = { ...getTestInvoice(sellerId, buyerId, paymentId), additionalDocumentReferences: [] };
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const logoBase64 = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==';
            const logoId = 'logo-123';

            const result = await invoiceRepository.addReplaceOrDeleteSellerLogo(id, logoBase64, logoId);

            expect(result).not.toBeNull();
            expect(result?.id).toBe(logoId);
            expect(result?.documentDescription).toBe('Seller Logo');
            expect(result?.mimeCode).toBe('image/png');
            expect(result?.fileName).toBe('SellerLogo.png');

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            expect(updatedInvoice?.additionalDocumentReferences).toHaveLength(1);
            expect(updatedInvoice?.additionalDocumentReferences[0].documentDescription).toBe('Seller Logo');
        });

        it('should replace existing seller logo', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            // Add initial logo
            const logo1 = 'logo-data-1';
            await invoiceRepository.addReplaceOrDeleteSellerLogo(id, logo1, 'logo-1');

            // Replace with new logo
            const logo2 = 'logo-data-2';
            const result = await invoiceRepository.addReplaceOrDeleteSellerLogo(id, logo2, 'logo-2');

            expect(result?.id).toBe('logo-2');
            expect(result?.content).toBe(logo2);

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            const logoDoc = updatedInvoice?.additionalDocumentReferences.find(doc => doc.documentDescription === 'Seller Logo');
            expect(logoDoc?.content).toBe(logo2);
        });

        it('should delete seller logo', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            // Add logo first
            await invoiceRepository.addReplaceOrDeleteSellerLogo(id, 'logo-data', 'logo-1');

            // Delete logo
            const result = await invoiceRepository.addReplaceOrDeleteSellerLogo(id);
            expect(result).toBeNull();

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            const logoDoc = updatedInvoice?.additionalDocumentReferences.find(doc => doc.documentDescription === 'Seller Logo');
            expect(logoDoc).toBeUndefined();
        });

        it('should add PDF document reference', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = { ...getTestInvoice(sellerId, buyerId, paymentId), additionalDocumentReferences: [] };
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const pdfBase64 = 'data:application/pdf;base64,JVBERi0xLjQKJcfsj6IK';
            const fileName = 'test-invoice.pdf';

            const result = await invoiceRepository.addReplaceOrDeletePdf(id, pdfBase64, fileName);

            expect(result).not.toBeNull();
            expect(result?.documentDescription).toBe('Invoice PDF');
            expect(result?.mimeCode).toBe('application/pdf');
            expect(result?.fileName).toBe(fileName);

            const updatedInvoice = await invoiceRepository.getInvoice(id);
            expect(updatedInvoice?.additionalDocumentReferences).toHaveLength(1);
        });
    });

    describe('Relationship Queries', () => {
        it('should get invoices referencing a party', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice1 = { ...getTestInvoice(sellerId, buyerId, paymentId), invoiceId: 'INV-001' };
            const testInvoice2 = { ...getTestInvoice(sellerId, buyerId, paymentId), invoiceId: 'INV-002' };

            const id1 = await invoiceRepository.createInvoice(testInvoice1, sellerId, buyerId, paymentId);
            const id2 = await invoiceRepository.createInvoice(testInvoice2, sellerId, buyerId, paymentId);

            const referencingInvoices = await invoiceRepository.getInvoicesReferencingParty(sellerId);

            expect(referencingInvoices).toContain(id1);
            expect(referencingInvoices).toContain(id2);
        });

        it('should get invoices referencing a payment', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const referencingInvoices = await invoiceRepository.getInvoicesReferencingPayment(paymentId);

            expect(referencingInvoices).toContain(id);
        });
    });

    describe('Error Handling', () => {
        it('should handle getting non-existent invoice', async () => {
            const nonExistentId = 99999;
            const invoice = await invoiceRepository.getInvoice(nonExistentId);
            expect(invoice).toBeNull();
        });

        it('should throw error when updating non-existent invoice', async () => {
            const nonExistentId = 99999;
            const updateData = { invoiceId: 'NEW-ID' };

            await expect(invoiceRepository.updateInvoice(nonExistentId, updateData))
                .rejects.toThrow('Invoice with ID 99999 not found');
        });

        it('should throw error when adding logo to non-existent invoice', async () => {
            const nonExistentId = 99999;
            const logoBase64 = 'test-logo';

            await expect(invoiceRepository.addReplaceOrDeleteSellerLogo(nonExistentId, logoBase64))
                .rejects.toThrow('Invoice with ID 99999 not found');
        });
    });

    describe('Data Integrity', () => {
        it('should not return soft deleted invoices in getAllInvoices', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = { ...getTestInvoice(sellerId, buyerId, paymentId), isDeleted: true };
            await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            const allInvoices = await invoiceRepository.getAllInvoices();
            expect(allInvoices.find(i => i.isDeleted === true)).toBeUndefined();
        });

        it('should not return soft deleted invoice in getInvoice', async () => {
            const { sellerId, buyerId, paymentId } = testData;
            const testInvoice = getTestInvoice(sellerId, buyerId, paymentId);
            const id = await invoiceRepository.createInvoice(testInvoice, sellerId, buyerId, paymentId);

            // Soft delete by updating
            await invoiceRepository.updateInvoice(id, { isDeleted: true });

            const deletedInvoice = await invoiceRepository.getInvoice(id);
            expect(deletedInvoice).toBeNull();
        });
    });
});
