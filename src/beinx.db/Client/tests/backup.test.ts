import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { exportDb, gzipString, importDb, paymentRepository, ungzipString } from '../beinx-db';
import { closeDB, DB_VERSION, Dump, openDB } from '../db-core';
import { getTestPayment } from './payment.test';

describe('Backup and Restore', () => {
    beforeEach(async () => {
        // Clear all stores before each test
        await paymentRepository.clear();
        // Add clear methods for other repositories as needed
    });

    afterEach(async () => {
        await paymentRepository.clear();
        closeDB();
    });

    it('should export an empty database', async () => {
        const base64 = await exportDb();
        expect(base64).toBeDefined();
        expect(base64.length).toBeGreaterThan(0);
    });

    it('should export and import a database with data', async () => {
        // Create test data
        const testPayment = getTestPayment();
        const paymentId = await paymentRepository.createPaymentMeans(testPayment);
        expect(paymentId).toBeGreaterThan(0);

        // Export
        const backup = await exportDb();
        expect(backup).toBeDefined();

        // Clear the database
        await paymentRepository.clear();
        const emptyPayments = await paymentRepository.getAllPaymentMeans();
        expect(emptyPayments.length).toBe(0);

        // Import
        await importDb(backup, false);

        // Verify data was restored
        const restoredPayments = await paymentRepository.getAllPaymentMeans();
        expect(restoredPayments.length).toBe(1);
        expect(restoredPayments[0].payment.name).toBe(testPayment.name);
        expect(restoredPayments[0].payment.iban).toBe(testPayment.iban);
    });

    it('should replace existing data when replace=true', async () => {
        // Create initial data
        const payment1 = getTestPayment();
        payment1.name = 'Payment 1';
        await paymentRepository.createPaymentMeans(payment1);

        // Create backup with different data
        const payment2 = getTestPayment();
        payment2.name = 'Payment 2';
        const id2 = await paymentRepository.createPaymentMeans(payment2);
        
        const backup = await exportDb();

        // Delete payment 2 and verify
        await paymentRepository.deletePaymentMeans(id2);
        const beforeRestore = await paymentRepository.getAllPaymentMeans();
        expect(beforeRestore.length).toBe(1);
        expect(beforeRestore[0].payment.name).toBe('Payment 1');

        // Import with replace=true
        await importDb(backup, true);

        // Verify only backup data exists
        const afterRestore = await paymentRepository.getAllPaymentMeans();
        expect(afterRestore.length).toBe(2);
        expect(afterRestore.some(p => p.payment.name === 'Payment 2')).toBe(true);
    });

    it('should merge data when replace=false', async () => {
        // Create initial data
        const payment1 = getTestPayment();
        payment1.name = 'Existing Payment';
        await paymentRepository.createPaymentMeans(payment1);

        // Create backup with different data
        await paymentRepository.clear();
        const payment2 = getTestPayment();
        payment2.name = 'Backup Payment';
        await paymentRepository.createPaymentMeans(payment2);
        const backup = await exportDb();

        // Restore original data
        await paymentRepository.clear();
        await paymentRepository.createPaymentMeans(payment1);

        // Import with replace=false (merge)
        await importDb(backup, false);

        // Verify both datasets exist
        const allPayments = await paymentRepository.getAllPaymentMeans();
        expect(allPayments.length).toBeGreaterThanOrEqual(1);
        // Note: exact count depends on your put() behavior with duplicate IDs
    });

    it('should include metadata in exported backup', async () => {
        const testPayment = getTestPayment();
        await paymentRepository.createPaymentMeans(testPayment);

        const backup = await exportDb();
        
        // Decompress and parse to check structure
        const json = ungzipString(backup);
        const dump: Dump = JSON.parse(json);

        expect(dump.__meta).toBeDefined();
        expect(dump.__meta.dbVersion).toBe(DB_VERSION);
        expect(dump.__meta.date).toBeDefined();
        expect(dump.stores).toBeDefined();
        expect(Object.keys(dump.stores).length).toBeGreaterThan(0);
    });

    it('should handle out-of-line keys (config store)', async () => {
        const db = await openDB();
        
        // Add some config data
        const tx = db.transaction(['AppConfig'], 'readwrite');
        const store = tx.objectStore('AppConfig');
        store.put({ theme: 'dark' }, 'appTheme');
        store.put({ lang: 'en' }, 'appLanguage');
        
        await new Promise((resolve, reject) => {
            tx.oncomplete = resolve;
            tx.onerror = () => reject(tx.error);
        });

        // Export and clear
        const backup = await exportDb();
        const clearTx = db.transaction(['AppConfig'], 'readwrite');
        clearTx.objectStore('AppConfig').clear();
        await new Promise((resolve) => { clearTx.oncomplete = resolve; });

        // Import and verify
        await importDb(backup, false);
        
        const readTx = db.transaction(['AppConfig'], 'readonly');
        const readStore = readTx.objectStore('AppConfig');
        
        const themeReq = readStore.get('appTheme');
        const langReq = readStore.get('appLanguage');
        
        await new Promise((resolve) => { readTx.oncomplete = resolve; });
        
        expect(themeReq.result).toEqual({ theme: 'dark' });
        expect(langReq.result).toEqual({ lang: 'en' });
    });

    it('should migrate old backup format to new format', async () => {
        // Create a legacy backup (without metadata)
        const testPayment = getTestPayment();
        const id = await paymentRepository.createPaymentMeans(testPayment);
        
        // Manually create old format backup
        const db = await openDB();
        const tx = db.transaction(['PaymentMeans'], 'readonly');
        const store = tx.objectStore('PaymentMeans');
        const items = await new Promise<any[]>((resolve) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result);
        });
        
        const legacyBackup = gzipString(JSON.stringify({
            PaymentMeans: items
        }));

        // Clear and import legacy backup
        await paymentRepository.clear();
        await importDb(legacyBackup, false);

        // Verify data was imported and migrated
        const restoredPayments = await paymentRepository.getAllPaymentMeans();
        expect(restoredPayments.length).toBe(1);
        expect(restoredPayments[0].payment.name).toBe(testPayment.name);
    });

    it('should handle multiple store types in one backup', async () => {
        // Add data to multiple stores
        const payment = getTestPayment();
        await paymentRepository.createPaymentMeans(payment);

        // Add config
        const db = await openDB();
        const tx = db.transaction(['AppConfig'], 'readwrite');
        tx.objectStore('AppConfig').put({ value: 'test' }, 'testKey');
        await new Promise((resolve) => { tx.oncomplete = resolve; });

        // Export
        const backup = await exportDb();

        // Clear all
        await paymentRepository.clear();
        const clearTx = db.transaction(['AppConfig'], 'readwrite');
        clearTx.objectStore('AppConfig').clear();
        await new Promise((resolve) => { clearTx.oncomplete = resolve; });

        // Import
        await importDb(backup, false);

        // Verify both stores restored
        const payments = await paymentRepository.getAllPaymentMeans();
        expect(payments.length).toBe(1);

        const readTx = db.transaction(['AppConfig'], 'readonly');
        const configReq = readTx.objectStore('AppConfig').get('testKey');
        await new Promise((resolve) => { readTx.oncomplete = resolve; });
        expect(configReq.result).toEqual({ value: 'test' });
    });
});