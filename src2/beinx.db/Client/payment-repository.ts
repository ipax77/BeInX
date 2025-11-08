import { openDB, STORES } from "./db-core";
import { IPaymentMeansBaseDto, PaymentMeansEntity } from "./dtos";

export class PaymentRepository {

    async createPaymentMeans(paymentMeans: IPaymentMeansBaseDto): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, 'readwrite');
        const store = transaction.objectStore(STORES.payments);
        const payment: Omit<PaymentMeansEntity, "id"> = {
            payment: paymentMeans,
        };

        return new Promise((resolve, reject) => {
            const req = store.add(payment);
            req.onsuccess = () => resolve(req.result as number);
            req.onerror = () => reject(req.error);
        });
    }

    async updatePaymentMeans(payment: PaymentMeansEntity): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readwrite");
        const store = transaction.objectStore(STORES.payments);

        return new Promise((resolve, reject) => {
            const req = store.put(payment);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async deletePaymentMeans(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readwrite");
        const store = transaction.objectStore(STORES.payments);

        return new Promise((resolve, reject) => {
            const req = store.delete(id);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
        });
    }

    async getAllPaymentMeans(): Promise<PaymentMeansEntity[]> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readonly");
        const store = transaction.objectStore(STORES.payments);

        return new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result as PaymentMeansEntity[]);
            req.onerror = () => reject(req.error);
        });
    }

    async clear(): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.payments], "readwrite");
        const store = transaction.objectStore(STORES.payments);
        return new Promise((resolve, reject) => {
            const request = store.clear();
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }
}