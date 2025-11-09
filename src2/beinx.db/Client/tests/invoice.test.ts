
import { describe, it, expect, afterEach } from 'vitest';
import { FinalizeResult, InvoiceDto, InvoiceDtoInfo } from '../dtos';
import { invoiceRepository, partyRepository, paymentRepository } from '../beinx-db';
import { getTestPayment } from './payment.test';
import { getTestParty } from './party.test';

const getTestInvoice = ():InvoiceDto => {
    return {
    globalTaxCategory: 'S',
    globalTaxScheme: 'VAT',
    globalTax: 19.0,
    id: '20251109-1',
    issueDate: '2025-11-09T00:00:00.000Z',
    invoiceTypeCode: '380',
    documentCurrencyCode: 'EUR',
    paymentTermsNote: 'Zahlbar innerhalb von 14 Tagen.',
    payableAmount: 100.0,
    additionalDocumentReferences: [],
    sellerParty: getTestParty(),
    buyerParty: getTestParty(),
    paymentMeans: getTestPayment(),
    invoiceLines: []
};
}

const getTestInvoiceInfo = async (): Promise<InvoiceDtoInfo> => {
    const paymentId = await paymentRepository.createPaymentMeans(getTestPayment());
    const sellerId = await partyRepository.createParty(getTestParty(), true);
    const buyerId = await partyRepository.createParty(getTestParty(), false);
    const info: InvoiceDtoInfo = {
        invoiceDto: getTestInvoice(),
        paymentId,
        sellerId,
        buyerId
    };
    return info;
}

describe('invoices CRUD', () => {
    afterEach(async () => {
        await invoiceRepository.clear();
    });

    it('should create an invoice', async () => {
        const invoiceInfo = await getTestInvoiceInfo();
        const id = await invoiceRepository.createInvoice(invoiceInfo, false);
        expect(id).toBeGreaterThan(0);

        const createdInvoices = await invoiceRepository.getAllInvoices();
        expect(createdInvoices.shift()?.id).toEqual(id);
    });

     it('should update an invoice', async () => {
    const invoiceInfo = await getTestInvoiceInfo();
    const id = await invoiceRepository.createInvoice(invoiceInfo, false);

    await invoiceRepository.updateInvoice(id, {
      invoiceDto: { ...invoiceInfo.invoiceDto, payableAmount: 250 }
    });

    const updated = await invoiceRepository.getById(id);
    expect(updated?.info.invoiceDto.payableAmount).toEqual(250);
  });

  it('should mark invoice as paid', async () => {
    const invoiceInfo = await getTestInvoiceInfo();
    const id = await invoiceRepository.createInvoice(invoiceInfo, false);

    await invoiceRepository.markAsPaid(id);
    const updated = await invoiceRepository.getById(id);
    expect(updated?.isPaid).toBe(true);
  });

  it('should finalize an invoice', async () => {
    const invoiceInfo = await getTestInvoiceInfo();
    const id = await invoiceRepository.createInvoice(invoiceInfo, false);

    const finalizeResult: FinalizeResult = {
      xmlInvoiceCreated: '<xml>Invoice</xml>',
      xmlInvoiceSha1Hash: 'abc123hash',
      xmlInvoiceBlob: new TextEncoder().encode('<xml>Invoice</xml>').buffer,
    };

    await invoiceRepository.finalizeInvoice(id, finalizeResult);

    const updated = await invoiceRepository.getById(id);
    expect(updated?.finalizeResult?.xmlInvoiceSha1Hash).toBe('abc123hash');
  });

  it('should count invoices and support pagination', async () => {
    // Create several invoices
    for (let i = 0; i < 10; i++) {
      const info = await getTestInvoiceInfo();
      info.invoiceDto.id = `INV-${i}`;
      await invoiceRepository.createInvoice(info, false);
    }

    const count = await invoiceRepository.getInvoiceCount();
    expect(count).toBeGreaterThanOrEqual(10);

    const firstPage = await invoiceRepository.getInvoiceList(5, 0);
    const secondPage = await invoiceRepository.getInvoiceList(5, 5);

    expect(firstPage.length).toBe(5);
    expect(secondPage.length).toBe(5);
    expect(firstPage[0].id).not.toEqual(secondPage[0].id);
  });

  it('should delete an invoice', async () => {
    const invoiceInfo = await getTestInvoiceInfo();
    const id = await invoiceRepository.createInvoice(invoiceInfo, false);

    await invoiceRepository.deleteInvoice(id);

    const found = await invoiceRepository.getById(id);
    expect(found).toBeNull();

    const total = await invoiceRepository.getInvoiceCount();
    expect(total).toEqual(0);
  });
});
