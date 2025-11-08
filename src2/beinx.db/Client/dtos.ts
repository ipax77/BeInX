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
    isSeller: boolean;
    logo?: DocumentReferenceAnnotationDto;
    createdAt: string; 
    updatedAt: string;
}

export interface IDraft {
    id: string; // entityType + entityId or 'draft'
    entityType: string,
    entityId?: number,
    data: IPaymentMeansBaseDto | IPartyBaseDto,
    updatedAt: string;
};