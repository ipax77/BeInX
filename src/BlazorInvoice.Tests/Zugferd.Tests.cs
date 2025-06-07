using BlazorInvoice.Shared;
using BlazorInvoice.Shared.ZUGFeRD;
using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.Tests
{
    [TestClass]
    public sealed class ZugferdTests
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
                SellerParty = new SellerAnnotationDto()
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
                    CompanyId = "000/0000/000"
                },
                BuyerParty = new BuyerAnnotationDto()
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
                PaymentMeans = new PaymentAnnotationDto()
                {
                    Iban = "DE12 1234 1234 1234 1234 12",
                    Bic = "BICABCDE",
                    Name = "Bank Name",
                    PaymentMeansTypeCode = "30",
                },
                PaymentTermsNote = "Zahlbar innerhalb 14 Tagen nach Erhalt der Rechnung.",
                PayableAmount = 119.0,
                InvoiceLines = [
                    new InvoiceLineAnnotationDto()
                {
                    Id = "1",
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
            var xmlText = ZugferdMapper.MapToZugferd(invoice);
            Assert.IsNotNull(xmlText, "Mapping to XML failed.");
            Assert.IsTrue(xmlText.Contains("Test Job"), "Invoice line description not found in XML.");
            Assert.IsTrue(xmlText.Contains("Buyer Name"), "Buyer name not found in XML.");
            Assert.IsTrue(xmlText.Contains("Seller Name"), "Seller name not found in XML.");
            Assert.IsTrue(xmlText.Contains("Zahlbar innerhalb 14 Tagen"), "Payment terms not included.");

        }

        [TestMethod]
        public void CanMapNoTaxDto()
        {
            var invoice = GetInvoiceAnnDto();
            invoice.GlobalTax = 0.0; // Set tax to 0 for testing
            var xmlText = ZugferdMapper.MapToZugferd(invoice);
            Assert.IsNotNull(xmlText, "Mapping to XML failed.");
            Assert.IsTrue(xmlText.Contains("Test Job"), "Invoice line description not found in XML.");
            Assert.IsTrue(xmlText.Contains("Buyer Name"), "Buyer name not found in XML.");
            Assert.IsTrue(xmlText.Contains("Seller Name"), "Seller name not found in XML.");
            Assert.IsTrue(xmlText.Contains("Zahlbar innerhalb 14 Tagen"), "Payment terms not included.");

        }
    }
}
