import { openDB, STORES } from "./beinx-db.js";
import { InvoiceEntity, FinalizeResult, DocumentReferenceEntity } from "./dtos.js";

export class InvoiceRepository {
    
    /**
     * Gets an invoice by ID with all embedded data
     */
    async getInvoice(invoiceId: number): Promise<InvoiceEntity | null> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readonly");
        const store = transaction.objectStore(STORES.invoices);
        
        return new Promise((resolve, reject) => {
            const request = store.get(invoiceId);
            
            request.onsuccess = () => {
                const invoice = request.result as InvoiceEntity;
                if (!invoice || invoice.isDeleted) {
                    resolve(null);
                    return;
                }
                resolve(invoice);
            };
            
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Gets all invoices (for C# filtering/pagination)
     */
    async getAllInvoices(): Promise<InvoiceEntity[]> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readonly");
        const store = transaction.objectStore(STORES.invoices);
        
        return new Promise((resolve, reject) => {
            const invoices: InvoiceEntity[] = [];
            const cursor = store.openCursor();
            
            cursor.onsuccess = (event) => {
                const result = (event.target as IDBRequest).result;
                if (result) {
                    const invoice = result.value as InvoiceEntity;
                    if (!invoice.isDeleted) {
                        invoices.push(invoice);
                    }
                    result.continue();
                } else {
                    resolve(invoices);
                }
            };
            
            cursor.onerror = () => reject(cursor.error);
        });
    }

    /**
     * Creates a new invoice
     */
    async createInvoice(
        invoiceData: Omit<InvoiceEntity, 'id'>,
        sellerId: number,
        buyerId: number,
        paymentId: number,
        isImported: boolean = false
    ): Promise<number> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readwrite");
        const store = transaction.objectStore(STORES.invoices);
        
        const invoice: Omit<InvoiceEntity, 'id'> = {
            ...invoiceData,
            sellerPartyId: sellerId,
            buyerPartyId: buyerId,
            paymentMeansId: paymentId,
            isPaid: false,
            isDeleted: false,
            invoiceLines: invoiceData.invoiceLines || [],
            additionalDocumentReferences: invoiceData.additionalDocumentReferences || []
        };
        
        return new Promise((resolve, reject) => {
            const request = store.add(invoice);
            
            request.onsuccess = () => {
                resolve(request.result as number);
            };
            
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Updates an existing invoice
     */
    async updateInvoice(invoiceId: number, invoiceData: Partial<InvoiceEntity>): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readwrite");
        const store = transaction.objectStore(STORES.invoices);
        
        return new Promise((resolve, reject) => {
            const getRequest = store.get(invoiceId);
            
            getRequest.onsuccess = () => {
                const existingInvoice = getRequest.result as InvoiceEntity;
                if (!existingInvoice) {
                    reject(new Error(`Invoice with ID ${invoiceId} not found`));
                    return;
                }
                
                // Update properties, preserving embedded collections if not provided
                const updatedInvoice: InvoiceEntity = {
                    ...existingInvoice,
                    ...invoiceData,
                    id: invoiceId, // Ensure ID stays the same
                    invoiceLines: invoiceData.invoiceLines || existingInvoice.invoiceLines,
                    additionalDocumentReferences: invoiceData.additionalDocumentReferences || existingInvoice.additionalDocumentReferences
                };
                
                const putRequest = store.put(updatedInvoice);
                putRequest.onsuccess = () => resolve();
                putRequest.onerror = () => reject(putRequest.error);
            };
            
            getRequest.onerror = () => reject(getRequest.error);
        });
    }

    /**
     * Deletes an invoice (hard delete since it's the top-level entity)
     */
    async deleteInvoice(invoiceId: number): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readwrite");
        const store = transaction.objectStore(STORES.invoices);
        
        return new Promise((resolve, reject) => {
            const request = store.delete(invoiceId);
            
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Sets the paid status of an invoice
     */
    async setIsPaid(invoiceId: number, isPaid: boolean): Promise<void> {
        return this.updateInvoice(invoiceId, { isPaid });
    }

    /**
     * Finalizes an invoice with XML data
     */
    async finalizeInvoice(
        invoiceId: number, 
        xmlInvoiceBlob: ArrayBuffer, 
        xmlInvoiceSha1Hash: string,
        totalAmountWithoutVat: number
    ): Promise<FinalizeResult> {
        const xmlInvoiceCreated = new Date().toISOString();
        
        await this.updateInvoice(invoiceId, {
            xmlInvoiceCreated,
            xmlInvoiceSha1Hash,
            xmlInvoiceBlob,
            totalAmountWithoutVat
        });

        return {
            xmlInvoiceCreated,
            xmlInvoiceSha1Hash,
            xmlInvoiceBlob
        };
    }

    /**
     * Gets the XML blob for an invoice
     */
    async getXmlBlob(invoiceId: number): Promise<ArrayBuffer | null> {
        const invoice = await this.getInvoice(invoiceId);
        return invoice?.xmlInvoiceBlob || null;
    }

    /**
     * Validates the XML invoice hash
     */
    async validateXmlInvoiceHash(invoiceId: number): Promise<boolean> {
        const invoice = await this.getInvoice(invoiceId);
        
        if (!invoice?.xmlInvoiceBlob || !invoice.xmlInvoiceSha1Hash) {
            return false;
        }

        // Compute SHA1 hash of the XML blob
        const hashBuffer = await crypto.subtle.digest('SHA-1', invoice.xmlInvoiceBlob);
        const hashArray = new Uint8Array(hashBuffer);
        const computedHash = btoa(String.fromCharCode(...hashArray));

        return computedHash === invoice.xmlInvoiceSha1Hash;
    }

    /**
     * Creates a copy of an invoice (for C# to handle business logic)
     */
    async getInvoiceForCopy(invoiceId: number): Promise<InvoiceEntity | null> {
        // Just return the invoice data - let C# handle the copy logic
        return this.getInvoice(invoiceId);
    }

    /**
     * Adds, replaces, or deletes a seller logo document reference within an invoice
     */
    async addReplaceOrDeleteSellerLogo(
        invoiceId: number, 
        logoBase64?: string, 
        logoId?: string
    ): Promise<DocumentReferenceEntity | null> {
        const invoice = await this.getInvoice(invoiceId);
        if (!invoice) {
            throw new Error(`Invoice with ID ${invoiceId} not found`);
        }

        const desc = "Seller Logo";
        const existingDocIndex = invoice.additionalDocumentReferences.findIndex(
            doc => doc.documentDescription === desc
        );

        if (!logoBase64) {
            // Delete the logo if it exists
            if (existingDocIndex >= 0) {
                invoice.additionalDocumentReferences.splice(existingDocIndex, 1);
                await this.updateInvoice(invoiceId, { additionalDocumentReferences: invoice.additionalDocumentReferences });
            }
            return null;
        } else {
            // Add or replace the logo
            const logoDoc: DocumentReferenceEntity = {
                id: logoId || crypto.randomUUID(),
                mimeCode: "image/png",
                documentDescription: desc,
                fileName: "SellerLogo.png",
                content: logoBase64
            };

            if (existingDocIndex >= 0) {
                // Replace existing
                invoice.additionalDocumentReferences[existingDocIndex] = logoDoc;
            } else {
                // Add new
                invoice.additionalDocumentReferences.push(logoDoc);
            }

            await this.updateInvoice(invoiceId, { additionalDocumentReferences: invoice.additionalDocumentReferences });
            return logoDoc;
        }
    }

    /**
     * Adds or replaces a PDF document reference
     */
    async addReplaceOrDeletePdf(
        invoiceId: number,
        pdfBase64?: string,
        fileName?: string
    ): Promise<DocumentReferenceEntity | null> {
        const invoice = await this.getInvoice(invoiceId);
        if (!invoice) {
            throw new Error(`Invoice with ID ${invoiceId} not found`);
        }

        const desc = "Invoice PDF";
        const existingDocIndex = invoice.additionalDocumentReferences.findIndex(
            doc => doc.documentDescription === desc
        );

        if (!pdfBase64) {
            // Delete the PDF if it exists
            if (existingDocIndex >= 0) {
                invoice.additionalDocumentReferences.splice(existingDocIndex, 1);
                await this.updateInvoice(invoiceId, { additionalDocumentReferences: invoice.additionalDocumentReferences });
            }
            return null;
        } else {
            // Add or replace the PDF
            const pdfDoc: DocumentReferenceEntity = {
                id: crypto.randomUUID(),
                mimeCode: "application/pdf",
                documentDescription: desc,
                fileName: fileName || "invoice.pdf",
                content: pdfBase64
            };

            if (existingDocIndex >= 0) {
                // Replace existing
                invoice.additionalDocumentReferences[existingDocIndex] = pdfDoc;
            } else {
                // Add new
                invoice.additionalDocumentReferences.push(pdfDoc);
            }

            await this.updateInvoice(invoiceId, { additionalDocumentReferences: invoice.additionalDocumentReferences });
            return pdfDoc;
        }
    }

    /**
     * Gets all invoices that reference a specific party (for checking if party can be deleted)
     */
    async getInvoicesReferencingParty(partyId: number): Promise<number[]> {
        const allInvoices = await this.getAllInvoices();
        return allInvoices
            .filter(invoice => invoice.sellerPartyId === partyId || invoice.buyerPartyId === partyId)
            .map(invoice => invoice.id!)
            .filter(id => id !== undefined);
    }

    /**
     * Gets all invoices that reference a specific payment means
     */
    async getInvoicesReferencingPayment(paymentId: number): Promise<number[]> {
        const allInvoices = await this.getAllInvoices();
        return allInvoices
            .filter(invoice => invoice.paymentMeansId === paymentId)
            .map(invoice => invoice.id!)
            .filter(id => id !== undefined);
    }

    async clear(): Promise<void> {
        const db = await openDB();
        const transaction = db.transaction([STORES.invoices], "readwrite");
        const store = transaction.objectStore(STORES.invoices);
        return new Promise((resolve, reject) => {
            const request = store.clear();
            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    }
}