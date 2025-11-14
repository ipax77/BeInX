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

export interface IPaymentMeansBaseDto {
    name: string;
    iban: string;
    bic: string;
    paymentMeansTypeCode: string;
}

export interface PaymentMeansEntity {
    id?: number;
    payment: IPaymentMeansBaseDto;
    createdAt: string; 
    updatedAt: string;
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

export interface DocumentReferenceAnnotationDto {
    id: string;
    documentDescription: string;
    mimeCode: string;
    fileName: string;
    content: string;
}

export interface PartyEntity {
    id?: number;
    party: IPartyBaseDto;
    logo?: DocumentReferenceAnnotationDto;
    createdAt: string; 
    updatedAt: string;
}

export interface IDraft {
    id: string; // entityType + entityId or 'draft'
    entityType: string,
    entityId?: number,
    data: IPaymentMeansBaseDto | IPartyBaseDto | InvoiceDtoInfo,
    updatedAt: string;
};

export interface InvoiceLineDto {
    id: string;
    note?: string;
    quantity: number;
    quantityCode: string;
    unitPrice: number;
    startDate?: string;
    endDate?: string;
    description?: string;
    name: string;
}

export interface InvoiceDto {
    globalTaxCategory: string;
    globalTaxScheme: string;
    globalTax: number;
    id: string;
    issueDate: string;
    dueDate?: string;
    invoiceTypeCode: string;
    note?: string;
    documentCurrencyCode: string;
    paymentTermsNote: string;
    payableAmount: number;
    additionalDocumentReferences: DocumentReferenceAnnotationDto[];
    sellerParty: IPartyBaseDto;
    buyerParty: IPartyBaseDto;
    paymentMeans: IPaymentMeansBaseDto;
    invoiceLines: InvoiceLineDto[];
}

export interface FinalizeResult {
    xmlInvoiceCreated: string;
    xmlInvoiceSha1Hash: string;
    xmlInvoiceBlob: ArrayBuffer;
}

export interface InvoiceDtoInfo {
    invoiceDto: InvoiceDto;
    sellerId: number;
    buyerId: number;
    paymentId: number;
}

export interface InvoiceEntity {
    id: number;
    info: InvoiceDtoInfo;
    year: number;
    isPaid: boolean;
    isImported: boolean;
    finalizeResult?: FinalizeResult;
    updatedAt: string;
}

export interface InvoiceListItem {
  id: number;
  invoiceId: string;
  issueDate: string;
  sellerName: string;
  buyerName: string;
  isPaid: boolean;
  year: number;
  payableAmount: number;
}

export interface InvoicesRequest {
    year: number | null;
    isPaid: boolean | null;
    search: string | null;
    page: number;
    pageSize: number;
    sortBy: 'invoiceId' | 'issueDate' | 'sellerName' | 'buyerName' | 'isPaid' | 'year';
    sortAsc: boolean;
}