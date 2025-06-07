using pax.XRechnung.NET.AnnotatedDtos;
using pax.XRechnung.NET.BaseDtos;
using s2industries.ZUGFeRD;
using System.Text;

namespace BlazorInvoice.Shared.ZUGFeRD;

public static class ZugferdMapper
{
    public static string MapToZugferd(BlazorInvoiceDto invoice)
    {
        decimal taxRate = InvoiceMapperUtils.RoundAmount(invoice.GlobalTax / 100.0);
        decimal taxExclusiveAmount = InvoiceMapperUtils.RoundAmount(invoice.InvoiceLines
            .Sum(s => InvoiceMapperUtils
                .RoundAmount(InvoiceMapperUtils.RoundAmount(s.Quantity)
                    * InvoiceMapperUtils.RoundAmount(s.UnitPrice))));

        decimal payableAmount = InvoiceMapperUtils.RoundAmount(taxExclusiveAmount + taxExclusiveAmount * taxRate);
        bool isSmallBusiness = taxRate == 0; // keine Umsatzsteuer nach § 19 UStG
        decimal taxAmount = isSmallBusiness ? 0 : InvoiceMapperUtils.RoundAmount(payableAmount - taxExclusiveAmount);


        InvoiceDescriptor desc = InvoiceDescriptor.CreateInvoice(
            invoice.Id,
            invoice.IssueDate,
            GetEnum<CurrencyCodes>(invoice.DocumentCurrencyCode)
        );
        desc.BusinessProcess = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";
        desc.Type = GetEnumFromAttributeValue<InvoiceType>(invoice.InvoiceTypeCode);

        desc.Name = "Invoice";
        desc.ReferenceOrderNo = invoice.BuyerParty.BuyerReference;
        desc.AddNote(invoice.Note);

        desc.SetBuyer(invoice.BuyerParty.Name,
            invoice.BuyerParty.PostCode,
            invoice.BuyerParty.City,
            invoice.BuyerParty.StreetName,
            GetEnum<CountryCodes>(invoice.BuyerParty.CountryCode),
            id: string.Empty
        );
        desc.AddBuyerTaxRegistration(invoice.BuyerParty.TaxId, TaxRegistrationSchemeID.VA);
        desc.SetBuyerElectronicAddress(invoice.BuyerParty.Email, ElectronicAddressSchemeIdentifiers.EM);
        desc.SetBuyerContact(
            name: invoice.BuyerParty.Name,
            emailAddress: invoice.BuyerParty.Email,
            phoneno: invoice.BuyerParty.Telefone
        );

        desc.SetSeller(invoice.SellerParty.Name,
            invoice.SellerParty.PostCode,
            invoice.SellerParty.City,
            invoice.SellerParty.StreetName,
            GetEnum<CountryCodes>(invoice.SellerParty.CountryCode),
            id: string.Empty
        );
        desc.AddSellerTaxRegistration(invoice.SellerParty.CompanyId, TaxRegistrationSchemeID.FC);
        desc.AddSellerTaxRegistration(invoice.SellerParty.TaxId, TaxRegistrationSchemeID.VA);
        desc.SetSellerContact(
            name: invoice.SellerParty.Name,
            emailAddress: invoice.SellerParty.Email,
            phoneno: invoice.SellerParty.Telefone
        );
        desc.SetSellerElectronicAddress(invoice.SellerParty.Email, ElectronicAddressSchemeIdentifiers.EM);

        desc.AddApplicableTradeTax(
            basisAmount: taxExclusiveAmount,
            percent: (decimal)invoice.GlobalTax,
            taxAmount: taxAmount,
            typeCode: GetEnum<TaxTypes>(invoice.GlobalTaxScheme),
            categoryCode: invoice.GlobalTax == 0 ? TaxCategoryCodes.E : GetEnum<TaxCategoryCodes>(invoice.GlobalTaxCategory),
            exemptionReason: invoice.GlobalTax == 0 ? "Kein Ausweis von Umsatzsteuer, da Kleinunternehmer gemäß § 19UStG" : null
        );

        desc.AddTradePaymentTerms(
            description: invoice.PaymentTermsNote,
            dueDate: invoice.DueDate
        );

        desc.AddCreditorFinancialAccount(
            iban: invoice.PaymentMeans.Iban,
            bic: invoice.PaymentMeans.Bic,
            bankName: invoice.PaymentMeans.Name,
            name: invoice.SellerParty.Name
        );

        desc.SetPaymentMeans(
            paymentCode: GetEnumFromAttributeValue<PaymentMeansTypeCodes>(invoice.PaymentMeans.PaymentMeansTypeCode)
        );

        foreach (var line in invoice.InvoiceLines.OrderBy(o => o.Id))
        {
            AddLine(line, invoice, desc);
        }

        desc.SetTotals(
            lineTotalAmount: taxExclusiveAmount,
            taxBasisAmount: taxExclusiveAmount,
            taxTotalAmount: taxAmount,
            grandTotalAmount: payableAmount,
            duePayableAmount: payableAmount
        );

        using var memoryStream = new MemoryStream();
        desc.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Basic);
        memoryStream.Position = 0;
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static void AddLine(InvoiceLineAnnotationDto line, BlazorInvoiceDto invoice, InvoiceDescriptor desc)
    {
        desc.AddTradeLineItem(
            name: line.Name,
            unitCode: GetEnum<QuantityCodes>(line.QuantityCode),
            grossUnitPrice: (decimal)line.UnitPrice,
            netUnitPrice: (decimal)line.UnitPrice,
            description: line.Description,
            billedQuantity: (decimal)line.Quantity,
            billingPeriodStart: line.StartDate,
            billingPeriodEnd: line.EndDate,
            taxType: GetEnum<TaxTypes>(invoice.GlobalTaxScheme),
            categoryCode: invoice.GlobalTax == 0 ? TaxCategoryCodes.E : GetEnum<TaxCategoryCodes>(invoice.GlobalTaxCategory),
            taxPercent: (decimal)invoice.GlobalTax
        );
    }

    private static T GetEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, out var result))
        {
            return result;
        }
        throw new ArgumentException($"Invalid value '{value}' for enum type '{typeof(T).Name}'");
    }

    public static T GetEnumFromAttributeValue<T>(string code) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = Attribute.GetCustomAttribute(field, typeof(EnumStringValueAttribute)) as EnumStringValueAttribute;
            if (attribute != null && attribute.Value == code)
            {
                return (T)field.GetValue(null)!;
            }
        }
        throw new ArgumentException($"Invalid code '{code}' for enum type '{typeof(T).Name}'");
    }
}
