
import { describe, it, expect, afterEach } from 'vitest';
import { IPaymentMeansBaseDto } from '../dtos';
import { paymentRepository } from '../beinx-db';

export const getTestPayment = (): IPaymentMeansBaseDto => ({
    name: 'Test Payment',
    iban: 'DE12345678901234567890',
    bic: 'TESTDEFFXXX',
    paymentMeansTypeCode: '30',
});

describe('payments CRUD', () => {
    afterEach(async () => {
        await paymentRepository.clear();
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
        await paymentRepository.updatePaymentMeans(id, payment.payment);
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

    describe('temp payment drafts', () => {
        afterEach(async () => {
            await paymentRepository.clear();
        });

        it('should save and retrieve a temp payment draft', async () => {
            const draftPayment = getTestPayment();
            draftPayment.name = "Temp Draft Test";

            await paymentRepository.saveTempPayment(draftPayment);

            const loadedDraft = await paymentRepository.loadTempPayment();
            expect(loadedDraft).toBeDefined();
            expect(loadedDraft?.data.name).toEqual("Temp Draft Test");
        });

        it('should overwrite an existing temp draft with same entityType', async () => {
            const draftPayment1 = getTestPayment();
            draftPayment1.name = "First Temp";
            await paymentRepository.saveTempPayment(draftPayment1);

            const draftPayment2 = getTestPayment();
            draftPayment2.name = "Second Temp";
            await paymentRepository.saveTempPayment(draftPayment2);

            const loadedDraft = await paymentRepository.loadTempPayment();
            expect(loadedDraft).toBeDefined();
            expect(loadedDraft?.data.name).toEqual("Second Temp"); // overwritten
        });

        it('should delete a temp payment draft', async () => {
            const draftPayment = getTestPayment();
            await paymentRepository.saveTempPayment(draftPayment);

            await paymentRepository.clearTempPayment();

            const loadedDraft = await paymentRepository.loadTempPayment();
            expect(loadedDraft).toBeNull();
        });
    });
});
