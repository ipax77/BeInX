
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
