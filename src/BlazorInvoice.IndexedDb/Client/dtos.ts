
export interface AppConfigDto {
    cultureName: string;
    backupFolder: string;
    backupInterval: number;
    schematronValidationUri: string;
    showFormDescriptions: boolean;
    showValidationWarnings: boolean;
    checkForUpdates: boolean;
    exportEmbedPdf: boolean;
    exportValidate: boolean;
    exportFinalize: boolean;
    exportType: number;
    statsMonthEndDay: number;
    statsIsMonthNotQuater: string;
}

export interface TableOrder {
    propertyName: string;
    ascending: boolean;
}

export interface InvoiceListRequest {
    filter: string;
    skip: number;
    take: number;
    tableOrders: TableOrder[];
}

export interface PaymentListDto {
    playmentMeansId: number;
    name: string;
    iban: string;
}

export interface IPaymentMeansBaseDto {
    name: string;
    iban: string;
    bic: string;
    paymentMeansTypeCode: string;
}

export interface PartyListDto {
    readonly partyId: number;
    readonly name: string;
    readonly email: string;
}

export interface DocumentReferenceAnnotationDto {
    id: string;
    documentDescription: string;
    mimeCode: string;
    fileName: string;
    content: string;
}

export interface IPartyBaseDto {
    website?: string;
    logoReferenceId?: string;
    name: string;
    streetName?: string;
    city: string;
    postCode: string;
    countryCode: string;
    telefone: string;
    email: string;
    registrationName: string;
    taxId: string;
    companyId?: string;
    buyerReference: string;
}

export interface SellerAnnotationDto extends IPartyBaseDto {}

export interface BuyerAnnotationDto extends IPartyBaseDto {}

// Internal party storage interface
export interface PartyEntity {
    id?: number; // Auto-increment key
    website?: string;
    logoReferenceId?: string;
    name: string;
    streetName?: string;
    city: string;
    postCode: string;
    countryCode: string;
    telefone: string;
    email: string;
    registrationName: string;
    taxId: string;
    companyId?: string;
    buyerReference: string;
    isSeller: boolean;
    isDeleted: boolean;
    logo?: string; // Base64 encoded image data
}

// TypeScript interfaces for Invoice operations
export interface InvoiceEntity {
    id?: number; // Auto-increment primary key (maps to SQL InvoiceId)
    
    // Core invoice data
    globalTaxCategory: string;
    globalTaxScheme: string;
    globalTax: number;
    invoiceId: string; // Business ID (maps to SQL Id field)
    issueDate: string; // ISO date string
    dueDate?: string;
    note?: string;
    invoiceTypeCode: string;
    documentCurrencyCode: string;
    paymentTermsNote: string;
    payableAmount: number;
    
    // Foreign key references
    sellerPartyId: number;
    buyerPartyId: number;
    paymentMeansId: number;
    
    // Embedded related data
    invoiceLines: InvoiceLineEntity[];
    additionalDocumentReferences: DocumentReferenceEntity[];
    
    // XML finalization data
    xmlInvoiceCreated?: string; // ISO date string
    xmlInvoiceSha1Hash?: string;
    xmlInvoiceBlob?: ArrayBuffer; // Binary XML data
    totalAmountWithoutVat?: number;
    
    // Status flags
    isPaid: boolean;
    isDeleted: boolean; // For soft delete
}

export interface InvoiceLineEntity {
    id: string; // Business ID
    note?: string;
    quantity: number;
    quantityCode: string;
    unitPrice: number;
    startDate?: string; // ISO date string
    endDate?: string; // ISO date string
    description?: string;
    name: string;
}

export interface DocumentReferenceEntity {
    id: string;
    documentDescription: string;
    mimeCode: string;
    fileName: string;
    content: string; // Base64 encoded
}

// Result interface matching C# InvoiceDtoInfo
export interface InvoiceDtoInfo {
    invoiceDto: any; // Will be mapped to BlazorInvoiceDto in C#
    invoiceId: number;
    sellerId?: number;
    buyerId?: number;
    paymentId?: number;
}

export interface FinalizeResult {
    xmlInvoiceCreated: string; // ISO date string
    xmlInvoiceSha1Hash: string;
    xmlInvoiceBlob: ArrayBuffer;
}