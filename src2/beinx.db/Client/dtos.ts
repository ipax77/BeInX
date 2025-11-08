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
