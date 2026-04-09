using beinx.shared;

namespace beinx.Tests;

[TestClass]
public sealed class MapTests
{
    public static BlazorInvoiceDto GetInvoiceAnnDto()
    {
        return new()
        {
            GlobalTaxCategory = "S",
            GlobalTaxScheme = "VAT",
            GlobalTax = 19.0,
            Id = "1",
            IssueDate = DateTime.UtcNow,
            InvoiceTypeCode = "380",
            DocumentCurrencyCode = "EUR",
            Note = "Test Note",
            SellerParty = new()
            {
                Name = "Seller Name",
                StreetName = "Test Street",
                City = "Test City",
                PostCode = "123456",
                CountryCode = "DE",
                Telefone = "1234/54321",
                Email = "seller@example.com",
                RegistrationName = "Seller Name",
                TaxId = "DE12345678",
                CompanyId = "000/000/0000 0",
            },
            BuyerParty = new()
            {
                Name = "Buyer Name",
                StreetName = "Test Street",
                AdditionalStreetName = "c/o test",
                City = "Test City",
                PostCode = "123456",
                CountryCode = "DE",
                Telefone = "1234/54321",
                Email = "buyer@example.com",
                RegistrationName = "Buyer Name",
                BuyerReference = "04011000-12345-34",
            },
            PaymentMeans = new()
            {
                Iban = "DE12 1234 1234 1234 1234 12",
                Bic = "BICABCDE",
                Name = "Bank Name",
                PaymentMeansTypeCode = "30",
            },
            PaymentTermsNote = "Zahlbar innerhalb von 14 Tagen nach Erhalt der Rechnung.",
            PayableAmount = 119.0,
            InvoiceLines = [
                new()
                {
                    Id = "1",
                    StartDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0),
                    EndDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0),
                    Quantity = 1.0,
                    QuantityCode = "HUR",
                    UnitPrice = 100.0,
                    Name = "Test Job"
                }
            ]
        };
    }

    [TestMethod]
    public void CanMapDto()
    {
        var invoice = GetInvoiceAnnDto();
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(invoice);
        Assert.AreEqual(invoice.BuyerParty.AdditionalStreetName, xmlInvoice.BuyerParty.Party.PostalAddress.AdditionalStreetName);
        var invoiceDto = mapper.FromXml(xmlInvoice);
        Assert.AreEqual(invoice.BuyerParty.AdditionalStreetName, invoiceDto.BuyerParty.AdditionalStreetName);
    }
}