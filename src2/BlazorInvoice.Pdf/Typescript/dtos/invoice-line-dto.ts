
export interface InvoiceLineDto
{
    id: string;
    note?: string;
    quantity: number;
    quantityCode: string;
    unitPrice: number;
    startDate: string;
    endDate: string;
    description?: string;
    name: string;
    lineTotal: number;
}
