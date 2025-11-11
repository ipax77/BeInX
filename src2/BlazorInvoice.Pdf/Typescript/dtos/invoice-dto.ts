import { AdditionalDocumentReferenceDto } from './additional-document-reference-dto';
import { InvoiceLineDto } from './invoice-line-dto';
import { PartyDto } from './party-dto';
import { PaymentMeansDto } from './payment-means-dto';

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
    additionalDocumentReferences: AdditionalDocumentReferenceDto[];
    buyerReference: string;
    sellerParty: PartyDto;
    buyerParty: PartyDto;
    paymentMeans: PaymentMeansDto;
    paymentTermsNote: string;
    payableAmount: number;
    invoiceLines: InvoiceLineDto[];
}

