import { describe, it, expect, afterEach } from 'vitest';
import { invoiceRepository, partyRepository, paymentRepository } from '../beinx-db';
import { InvoiceDtoInfo } from '../dtos';
import { getTestPayment } from './payment.test';
import { getTestBuyer, getTestSeller } from './parties.test';

const getTestInvoice = async(): Promise<InvoiceDtoInfo> => {
    const paymentId = await paymentRepository.createPaymentMeans(getTestPayment());
    const sellerId = await partyRepository.createParty(getTestSeller(), true, undefined);
    const buyerId = await partyRepository.createParty(getTestBuyer(), false, undefined);
    const info: InvoiceDtoInfo = {
        invoiceDto: undefined,
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

});
