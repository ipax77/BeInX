import { openDB, STORES } from "./db-core";
import { IPaymentMeansBaseDto, PaymentMeansEntity } from "./dtos";

export class PaymentRepository {

    async createPaymentMeans(paymentMeans: IPaymentMeansBaseDto): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readwrite");
        const store = transaction.objectStore(STORES.payments);

        const now = new Date().toISOString();

        const payment: Omit<PaymentMeansEntity, "id"> = {
        payment: paymentMeans,
        createdAt: now,
        updatedAt: now,
        };

        return new Promise((resolve, reject) => {
        const req = store.add(payment);

        req.onsuccess = () => resolve(req.result as number); 
        req.onerror = () => reject(req.error);
    });
    }

    async updatePaymentMeans(id: number, paymentMeans: IPaymentMeansBaseDto): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction(STORES.payments, "readwrite");
        const store = transaction.objectStore(STORES.payments);

        const existingReq = store.get(id);

        return new Promise((resolve, reject) => {
        existingReq.onsuccess = () => {
            const existing = existingReq.result as PaymentMeansEntity | undefined;
            if (!existing) {
            reject(new Error(`PaymentMeans with id ${id} not found`));
            return;
            }

            const updated: PaymentMeansEntity = {
            ...existing,
            payment: paymentMeans,
            updatedAt: new Date().toISOString(),
            };

            const updateReq = store.put(updated);
            updateReq.onsuccess = () => {
                resolve();
            };
            updateReq.onerror = () => {
                reject(updateReq.error);
            };
        };

        existingReq.onerror = () => reject(existingReq.error);
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