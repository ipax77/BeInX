
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