
import { describe, it, expect, afterEach } from 'vitest';
import { invoiceRepository, partyRepository, paymentRepository } from '../beinx-db';
import { IPaymentMeansBaseDto } from '../dtos';

export const getTestPayment = (): IPaymentMeansBaseDto => ({
    name: 'Test Payment',
    iban: 'DE12345678901234567890',
    bic: 'TESTDEFFXXX',
    paymentMeansTypeCode: '30',
});

describe('payments CRUD', () => {
    afterEach(async () => {
        await paymentRepository.clear();
        await partyRepository.clear();
        await invoiceRepository.clear();
    });

    it('should create a payment', async () => {
        const testPayment = getTestPayment();
        const id = await paymentRepository.createPaymentMeans(testPayment);
        expect(id).toBeGreaterThan(0);

        const createdPayments = await paymentRepository.getAllPaymentMeans();
        expect(createdPayments.shift()?.id).toEqual(id);
    });

    it('should update a payment', async () => {
        const testPayment = getTestPayment();
        const id = await paymentRepository.createPaymentMeans(testPayment);

        const createdPayments = await paymentRepository.getAllPaymentMeans();
        const payment = createdPayments.find(f => f.id === id);
        expect(payment).toBeDefined();
        if (!payment) return;
        const newName = "Test Payment Update";
        payment.payment.name = newName;
        await paymentRepository.updatePaymentMeans(payment);
        const updatedPayments = await paymentRepository.getAllPaymentMeans();
        const updatedPayment = updatedPayments.find(f => f.id === id);
        expect(updatedPayment).toBeDefined();
        expect(updatedPayment?.payment.name).toEqual(newName);
    });

    it('should delete a payment', async () => {
        const testPayment = getTestPayment();
        const id = await paymentRepository.createPaymentMeans(testPayment);

        await paymentRepository.deletePaymentMeans(id);
        const createdPayments = await paymentRepository.getAllPaymentMeans();
        const deletedPayment = createdPayments.find(f => f.id === id);
        expect(deletedPayment).toBeUndefined();
    });
});
