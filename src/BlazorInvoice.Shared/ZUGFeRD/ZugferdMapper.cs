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
        desc.Type = GetEnumFromAttributeValue<InvoiceType>(invoice.InvoiceTypeCode);

        desc.Name = "Invoice";
        desc.ReferenceOrderNo = invoice.BuyerParty.BuyerReference;
        desc.AddNote(invoice.Note);

        desc.SetBuyer(invoice.BuyerParty.RegistrationName,
            invoice.BuyerParty.PostCode,
            invoice.BuyerParty.City,
            invoice.BuyerParty.StreetName,
            GetEnum<CountryCodes>(invoice.BuyerParty.CountryCode)
        );
        desc.AddBuyerTaxRegistration(invoice.BuyerParty.BuyerReference, TaxRegistrationSchemeID.VA);
        desc.SetBuyerContact(
            name: invoice.BuyerParty.Name,
            emailAddress: invoice.BuyerParty.Email,
            phoneno: invoice.BuyerParty.Telefone
        );

        desc.SetSeller(invoice.SellerParty.Name,
            invoice.SellerParty.PostCode,
            invoice.SellerParty.City,
            invoice.SellerParty.StreetName,
            GetEnum<CountryCodes>(invoice.SellerParty.CountryCode)
        );
        desc.AddSellerTaxRegistration(invoice.SellerParty.CompanyId, TaxRegistrationSchemeID.FC);
        desc.AddSellerTaxRegistration(invoice.SellerParty.TaxId, TaxRegistrationSchemeID.VA);
        desc.SetSellerContact(
            name: invoice.SellerParty.Name,
            emailAddress: invoice.SellerParty.Email,
            phoneno: invoice.SellerParty.Telefone
        );
        desc.SetSellerElectronicAddress(invoice.SellerParty.Email, ElectronicAddressSchemeIdentifiers.EM);

        desc.SetTotals(
            lineTotalAmount: taxExclusiveAmount
            );
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
            bankName: invoice.PaymentMeans.Name
        );

        desc.SetPaymentMeans(
            paymentCode: GetEnumFromAttributeValue<PaymentMeansTypeCodes>(invoice.PaymentMeans.PaymentMeansTypeCode)
        );

        foreach (var line in invoice.InvoiceLines.OrderBy(o => o.Id))
        {
            AddLine(line, desc);
        }

        using var memoryStream = new MemoryStream();
        desc.Save(memoryStream, ZUGFeRDVersion.Version23);
        memoryStream.Position = 0;
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static void AddLine(InvoiceLineAnnotationDto line, InvoiceDescriptor desc)
    {
        desc.AddTradeLineItem(
            name: line.Name,
            netUnitPrice: (decimal)line.UnitPrice,
            unitCode: GetEnum<QuantityCodes>(line.QuantityCode),
            description: line.Description,
            unitQuantity: (decimal)line.Quantity,
            billedQuantity: (decimal)line.Quantity,
            billingPeriodStart: line.StartDate,
            billingPeriodEnd: line.EndDate
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
