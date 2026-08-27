using beinx.shared;
using pax.XRechnung.NET.AnnotatedDtos;
using s2industries.ZUGFeRD;
using System.Text;

namespace beinx.db.Services;

public static class ZugferdMapper
{
    public static string MapToZugferd(BlazorInvoiceDto invoice)
    {
        decimal taxRate = (decimal)invoice.GlobalTax / 100.0m;
        decimal taxExclusiveAmount = Math.Round((decimal)invoice.InvoiceLines.Sum(s => s.LineTotal), 2);
        decimal payableAmount = Math.Round(taxExclusiveAmount + taxExclusiveAmount * taxRate, 2);
        bool isSmallBusiness = taxRate == 0; // keine Umsatzsteuer nach § 19 UStG
        decimal taxAmount = isSmallBusiness ? 0 : Math.Round(payableAmount - taxExclusiveAmount, 2);

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
        desc.Buyer.AddressLine3 = invoice.BuyerParty.AdditionalStreetName;
        desc.AddBuyerTaxRegistration(invoice.BuyerParty.TaxId, TaxRegistrationSchemeID.VA);
        desc.SetBuyerElectronicAddress(invoice.BuyerParty.Email, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);
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
        desc.SetSellerElectronicAddress(invoice.SellerParty.Email, ElectronicAddressSchemeIdentifiers.ElectronicMailSmtp);

        desc.AddApplicableTradeTax(
            basisAmount: taxExclusiveAmount,
            percent: (decimal)invoice.GlobalTax,
            taxAmount: taxAmount,
            typeCode: GetEnum<TaxTypes>(invoice.GlobalTaxScheme),
            categoryCode: isSmallBusiness ? TaxCategoryCodes.E : GetEnum<TaxCategoryCodes>(invoice.GlobalTaxCategory),
            exemptionReason: isSmallBusiness ? "Kein Ausweis von Umsatzsteuer, da Kleinunternehmer gemäß § 19UStG" : null
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
        desc.Save(memoryStream, ZUGFeRDVersion.Version23, Profile.Comfort, ZUGFeRDFormats.CII);
        return Encoding.UTF8.GetString(
            memoryStream.GetBuffer().AsSpan(0, checked((int)memoryStream.Length)));
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
        if (EnumStringValueCache<T>.ValuesByCode.TryGetValue(code, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Invalid code '{code}' for enum type '{typeof(T).Name}'");
    }

    public static BlazorInvoiceDto MapFromZugferd(string xmlText)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlText));
        var desc = InvoiceDescriptor.Load(stream);

        var paymentTerm = desc.GetTradePaymentTerms().FirstOrDefault();
        var firstTradeLine = desc.TradeLineItems.FirstOrDefault();
        var creditorAccount = desc.GetCreditorFinancialAccounts().FirstOrDefault();
        var buyerTaxRegistrations = GetTaxRegistrationNumbers(desc.BuyerTaxRegistration);
        var sellerTaxRegistrations = GetTaxRegistrationNumbers(desc.SellerTaxRegistration);

        var dto = new BlazorInvoiceDto
        {
            Id = desc.InvoiceNo,
            IssueDate = desc.InvoiceDate ?? DateTime.Now,
            DocumentCurrencyCode = desc.Currency.ToString(),
            InvoiceTypeCode = GetEnumAttributeValue(desc.Type),
            Note = desc.Notes.FirstOrDefault()?.Content,
            PayableAmount = (double)(desc.DuePayableAmount ?? 0),
            BuyerParty = new BlazorBuyerAnnotationDto
            {
                Name = desc.Buyer?.Name ?? string.Empty,
                StreetName = desc.Buyer?.Street,
                AdditionalStreetName = desc.Buyer?.AddressLine3,
                PostCode = desc.Buyer?.Postcode ?? string.Empty,
                City = desc.Buyer?.City ?? string.Empty,
                CountryCode = desc.Buyer?.Country.ToString() ?? string.Empty,
                Email = desc.BuyerElectronicAddress?.Address ?? string.Empty,
                Telefone = desc.BuyerContact?.PhoneNo ?? string.Empty,
                TaxId = buyerTaxRegistrations.TaxId,
                CompanyId = buyerTaxRegistrations.CompanyId,
                BuyerReference = desc.ReferenceOrderNo ?? string.Empty
            },
            SellerParty = new SellerAnnotationDto
            {
                Name = desc.Seller?.Name ?? string.Empty,
                StreetName = desc.Seller?.Street ?? string.Empty,
                PostCode = desc.Seller?.Postcode ?? string.Empty,
                City = desc.Seller?.City ?? string.Empty,
                CountryCode = desc.Seller?.Country.ToString() ?? string.Empty,
                Email = desc.SellerElectronicAddress?.Address ?? string.Empty,
                Telefone = desc.SellerContact?.PhoneNo ?? string.Empty,
                TaxId = sellerTaxRegistrations.TaxId,
                CompanyId = sellerTaxRegistrations.CompanyId
            },
            GlobalTax = (double)(firstTradeLine?.TaxPercent ?? 0),
            GlobalTaxScheme = firstTradeLine?.TaxType.ToString() ?? string.Empty,
            GlobalTaxCategory = firstTradeLine?.TaxCategoryCode.ToString() ?? string.Empty,
            PaymentMeans = new PaymentAnnotationDto
            {
                Iban = creditorAccount?.IBAN ?? string.Empty,
                Bic = creditorAccount?.BIC ?? string.Empty,
                Name = creditorAccount?.BankName ?? string.Empty,
                PaymentMeansTypeCode = desc.PaymentMeans.TypeCode == null ? "30" : GetEnumAttributeValue(desc.PaymentMeans.TypeCode.Value)
            },
            PaymentTermsNote = paymentTerm?.Description ?? string.Empty,
            DueDate = paymentTerm?.DueDate,
            InvoiceLines = desc.TradeLineItems.Select((line, index) => new InvoiceLineAnnotationDto
            {
                Id = (index + 1).ToString(),
                Name = line.Name,
                Description = line.Description,
                Quantity = (double)line.BilledQuantity,
                QuantityCode = line.UnitCode.ToString() ?? string.Empty,
                UnitPrice = (double)line.NetUnitPrice,
                StartDate = line.BillingPeriodStart,
                EndDate = line.BillingPeriodEnd
            }).ToList()
        };
        return dto;
    }

    private static (string TaxId, string CompanyId) GetTaxRegistrationNumbers(
        IEnumerable<TaxRegistration>? registrations)
    {
        string taxId = string.Empty;
        string companyId = string.Empty;

        if (registrations == null)
        {
            return (taxId, companyId);
        }

        foreach (var registration in registrations)
        {
            if (registration.SchemeID == TaxRegistrationSchemeID.VA && taxId.Length == 0)
            {
                taxId = registration.No ?? string.Empty;
            }
            else if (registration.SchemeID == TaxRegistrationSchemeID.FC && companyId.Length == 0)
            {
                companyId = registration.No ?? string.Empty;
            }

            if (taxId.Length > 0 && companyId.Length > 0)
            {
                break;
            }
        }

        return (taxId, companyId);
    }

    private static string GetEnumAttributeValue<T>(T enumValue) where T : Enum
    {
        if (enumValue == null)
        {
            return string.Empty;
        }

        var name = enumValue.ToString();
        if (EnumStringValueCache<T>.CodesByName.TryGetValue(name, out var code))
        {
            return code;
        }

        return name;
    }

    private static class EnumStringValueCache<T> where T : Enum
    {
        internal static readonly IReadOnlyDictionary<string, T> ValuesByCode;
        internal static readonly IReadOnlyDictionary<string, string> CodesByName;

        static EnumStringValueCache()
        {
            var valuesByCode = new Dictionary<string, T>(StringComparer.Ordinal);
            var codesByName = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var field in typeof(T).GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(
                    field,
                    typeof(EnumStringValueAttribute)) as EnumStringValueAttribute;

                if (attribute == null)
                {
                    continue;
                }

                valuesByCode.TryAdd(attribute.Value, (T)field.GetValue(null)!);
                codesByName.TryAdd(field.Name, attribute.Value);
            }

            ValuesByCode = valuesByCode;
            CodesByName = codesByName;
        }
    }
}
