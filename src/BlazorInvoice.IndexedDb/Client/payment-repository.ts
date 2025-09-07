import { openDB, STORES } from "./db-core.js";
import { IPaymentMeansBaseDto, PaymentMeansEntity } from "./dtos.js";

export class PaymentRepository {

    async createPaymentMeans(paymentMeans: IPaymentMeansBaseDto): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, 'readwrite');
        const store = transaction.objectStore(STORES.payments);
        const payment: Omit<PaymentMeansEntity, "id"> = {
            payment: paymentMeans,
            isDeleted: false
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

    async softDeletePaymentMeans(id: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readwrite");
        const store = transaction.objectStore(STORES.payments);

        const req = store.get(id);
        return new Promise((resolve, reject) => {
            req.onsuccess = () => {
                const entity = req.result as PaymentMeansEntity | undefined;
                if (entity) {
                    entity.isDeleted = true;
                    store.put(entity).onsuccess = () => resolve();
                } else {
                    reject(new Error("Entity not found"));
                }
            };
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