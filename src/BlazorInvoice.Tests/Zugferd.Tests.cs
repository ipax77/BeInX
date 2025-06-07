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
                Note = "Dies ist eine Testrechnung.",
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
                    BuyerReference = "DE87654321"
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

        [TestMethod]
        public void CanMapMultipleItemsDto()
        {
            var invoice = GetInvoiceAnnDto();
            invoice.InvoiceLines.Clear();
            invoice.InvoiceLines.Add(new() { Id = "1", Name = "Test 1", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 3 });
            invoice.InvoiceLines.Add(new() { Id = "2", Name = "Test 2", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2.5 });
            invoice.InvoiceLines.Add(new() { Id = "3", Name = "Test 3", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2.75 });
            invoice.InvoiceLines.Add(new() { Id = "4", Name = "Test 4", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2 });
            invoice.InvoiceLines.Add(new() { Id = "5", Name = "Test 5", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2 });
            invoice.InvoiceLines.Add(new() { Id = "6", Name = "Test 6", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2 });
            invoice.InvoiceLines.Add(new() { Id = "7", Name = "Test 7", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 1.66666666666667 });
            invoice.InvoiceLines.Add(new() { Id = "8", Name = "Test 8", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 0.833333333333333 });
            invoice.InvoiceLines.Add(new() { Id = "9", Name = "Test 9", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 0.416666666666667 });
            invoice.InvoiceLines.Add(new() { Id = "10", Name = "Test 10", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 3 });
            invoice.InvoiceLines.Add(new() { Id = "11", Name = "Test 11", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 3.5 });
            invoice.InvoiceLines.Add(new() { Id = "12", Name = "Test 12", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 0.583333333333333 });
            invoice.InvoiceLines.Add(new() { Id = "13", Name = "Test 13", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 1.5 });
            invoice.InvoiceLines.Add(new() { Id = "14", Name = "Test 14", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 1.5 });
            invoice.InvoiceLines.Add(new() { Id = "15", Name = "Test 15", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 1.5 });
            invoice.InvoiceLines.Add(new() { Id = "16", Name = "Test 16", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 0.75 });
            invoice.InvoiceLines.Add(new() { Id = "17", Name = "Test 17", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 2.33333333333333 });
            invoice.InvoiceLines.Add(new() { Id = "18", Name = "Test 18", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 4.5 });
            invoice.InvoiceLines.Add(new() { Id = "19", Name = "Test 19", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 3.58333333333333 });
            invoice.InvoiceLines.Add(new() { Id = "20", Name = "Test 20", QuantityCode = "HUR", UnitPrice = 60.0, Quantity = 3.75 });


            var xmlText = ZugferdMapper.MapToZugferd(invoice);
            Assert.IsNotNull(xmlText, "Mapping to XML failed.");

        }
    }
}
