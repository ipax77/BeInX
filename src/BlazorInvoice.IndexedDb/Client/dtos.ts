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
    isDeleted: boolean;
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

export interface PartyEntity {
    id?: number;
    party: IPartyBaseDto;
    isSeller: boolean;
    isDeleted: boolean;
    logo?: DocumentReferenceAnnotationDto;
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

export interface InvoiceEntity {
    id: number;
    info: InvoiceDtoInfo;
    year: number;
    isPaid: boolean;
    isImported: boolean;
    isDeleted: boolean;
    finalizeResult?: FinalizeResult;
}

export interface TempInvoiceEntity {
    invoiceBlob: ArrayBuffer;
    invoiceId?: number;
    sellerPartyId?: number;
    buyerPartyId?: number;
    paymentMeansId?: number;
}