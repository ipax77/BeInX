import { describe, it, expect, afterEach } from 'vitest';
import { invoiceRepository, partyRepository, paymentRepository } from '../beinx-db';
import { InvoiceDtoInfo } from '../dtos';
import { getTestPayment } from './payment.test';
import { getTestBuyer, getTestSeller } from './parties.test';

const getTestInvoice = async (): Promise<InvoiceDtoInfo> => {
    const paymentId = await paymentRepository.createPaymentMeans(getTestPayment());
    const sellerId = await partyRepository.createParty(getTestSeller(), true, undefined);
    const buyerId = await partyRepository.createParty(getTestBuyer(), false, undefined);
    const info: InvoiceDtoInfo = {
        invoiceDto: {},
        invoiceId: 0,
        paymentId,
        sellerId,
        buyerId
    };
    return info;
}

describe('InvoiceRepository CRUD Operations', () => {
    afterEach(async () => {
        await paymentRepository.clear();
        await partyRepository.clear();
        await invoiceRepository.clear();
    });

    it('should create an invoice', async () => {
        const testInvoice = await getTestInvoice();
        const id = await invoiceRepository.createInvoice(testInvoice, new Date().getFullYear(), false, false, undefined);
        expect(id).toBeGreaterThan(0);

        const createdInvoices = await invoiceRepository.getAllInvoices();
        expect(createdInvoices.shift()?.id).toEqual(id);
    });

    it('should update an invoice', async () => {
        const testInvoice = await getTestInvoice();
        const id = await invoiceRepository.createInvoice(testInvoice, new Date().getFullYear(), false, false, undefined);

        const createdInvoices = await invoiceRepository.getAllInvoices();
        const invoice = createdInvoices.find(f => f.id === id);
        expect(invoice).toBeDefined();
        if (!invoice) return;
        const newName = "Test Invoice Update";
        invoice.info.invoiceDto.purpose = newName;
        await invoiceRepository.updateInvoice(invoice);
        const updatedInvoices = await invoiceRepository.getAllInvoices();
        const updatedInvoice = updatedInvoices.find(f => f.id === id);
        expect(updatedInvoice).toBeDefined();
        expect(updatedInvoice?.info.invoiceDto.purpose).toEqual(newName);
    });

    it('should delete an invoice', async () => {
        const testInvoice = await getTestInvoice();
        const id = await invoiceRepository.createInvoice(testInvoice, new Date().getFullYear(), false, false, undefined);

        await invoiceRepository.deleteInvoice(id);
        const createdInvoices = await invoiceRepository.getAllInvoices();
        const deletedInvoice = createdInvoices.find(f => f.id === id);
        expect(deletedInvoice).toBeUndefined();
    });
});
