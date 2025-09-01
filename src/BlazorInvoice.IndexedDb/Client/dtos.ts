
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