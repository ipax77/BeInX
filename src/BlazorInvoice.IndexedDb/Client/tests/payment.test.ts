
import { describe, it, expect } from 'vitest';
import { createPaymentMeans, getPaymentMeans, updatePaymentMeans, deletePaymentMeans } from '../beinx-db';
import { IPaymentMeansBaseDto } from '../dtos';

const getTestPayment = (): IPaymentMeansBaseDto => ({
    name: 'Test Payment',
    iban: 'DE12345678901234567890',
    bic: 'TESTDEFFXXX',
    paymentMeansTypeCode: '30',
});

describe('payments CRUD', () => {
    it('should create a payment', async () => {
        const testPayment = getTestPayment();
        const id = await createPaymentMeans(testPayment);
        expect(id).toBeGreaterThan(0);

        const createdPayment = await getPaymentMeans(id);
        expect(createdPayment).toEqual({ ...testPayment, id });
    });

    it('should update a payment', async () => {
        const testPayment = getTestPayment();
        const id = await createPaymentMeans(testPayment);

        const updatedPaymentDto: IPaymentMeansBaseDto = {
            ...testPayment,
            name: 'Updated Test Payment',
        };

        await updatePaymentMeans(id, updatedPaymentDto);

        const updatedPayment = await getPaymentMeans(id);
        expect(updatedPayment.name).toBe('Updated Test Payment');
    });

    it('should delete a payment', async () => {
        const testPayment = getTestPayment();
        const id = await createPaymentMeans(testPayment);

        await deletePaymentMeans(id);

        const deletedPayment = await getPaymentMeans(id);
        expect(deletedPayment).toBeUndefined();
    });
});
