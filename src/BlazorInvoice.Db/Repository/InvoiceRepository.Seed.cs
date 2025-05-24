using BlazorInvoice.Shared;

namespace BlazorInvoice.Db.Repository;

public partial class InvoiceRepository
{
    public async Task SeedTestInvoices(int count)
    {
        DateTime currentDate = new(DateTime.Today.Year, 1, 1);
        var dayStep = 365 / count;
        var mapper = new BlazorInvoiceMapper();
        for (int i = 0; i < count; i++)
        {
            var invoiceDto = GetInvoiceAnnDto();
            invoiceDto.IssueDate = currentDate;
            var xmlInvoice = mapper.ToXml(invoiceDto);
            await ImportInvoice(xmlInvoice);
            currentDate = currentDate.AddDays(dayStep);
        }
    }

    public async Task<int> SeedTestInvoice()
    {
        var invoiceDto = GetInvoiceAnnDto();
        var mapper = new BlazorInvoiceMapper();
        var xmlInvoice = mapper.ToXml(invoiceDto);
        return await ImportInvoice(xmlInvoice);
    }

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
                TaxId = "DE12345678"
            },
            BuyerParty = new()
            {
                Name = "Buyer Name",
                StreetName = "Test Street",
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
                    StartDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 8, 0, 0),
                    EndDate = new(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 9, 0, 0),
                    Quantity = 1.0,
                    QuantityCode = "HUR",
                    UnitPrice = 100.0,
                    Name = "Test Job"
                }
            ]
        };
    }
}