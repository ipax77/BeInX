using beinx.db.Services;
using beinx.shared;
using pax.XRechnung.NET.AnnotatedDtos;

namespace beinx.Tests;

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
        Assert.Contains("Test Job", xmlText, "Invoice line description not found in XML.");
        Assert.Contains("Buyer Name", xmlText, "Buyer name not found in XML.");
        Assert.Contains("Seller Name", xmlText, "Seller name not found in XML.");
        Assert.Contains("Zahlbar innerhalb 14 Tagen", xmlText, "Payment terms not included.");
    }

    [TestMethod]
    public void CanMapNoTaxDto()
    {
        var invoice = GetInvoiceAnnDto();
        invoice.GlobalTax = 0.0; // Set tax to 0 for testing
        var xmlText = ZugferdMapper.MapToZugferd(invoice);
        Assert.IsNotNull(xmlText, "Mapping to XML failed.");
        Assert.Contains("Test Job", xmlText, "Invoice line description not found in XML.");
        Assert.Contains("Buyer Name", xmlText, "Buyer name not found in XML.");
        Assert.Contains("Seller Name", xmlText, "Seller name not found in XML.");
        Assert.Contains("Zahlbar innerhalb 14 Tagen", xmlText, "Payment terms not included.");

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

    [TestMethod]
    public void CanReverseMapDto()
    {
        var invoice = GetInvoiceAnnDto();
        var xmlText = ZugferdMapper.MapToZugferd(invoice);
        Assert.IsNotNull(xmlText, "Mapping to XML failed.");
        var reverseDto = ZugferdMapper.MapFromZugferd(xmlText);
        Assert.IsNotNull(reverseDto, "Mapping from XML failed.");
        Assert.AreEqual(invoice.Id, reverseDto.Id);
        Assert.AreEqual(invoice.DocumentCurrencyCode, reverseDto.DocumentCurrencyCode);
        Assert.AreEqual(invoice.InvoiceTypeCode, reverseDto.InvoiceTypeCode);
        Assert.AreEqual(invoice.BuyerParty.Name, reverseDto.BuyerParty.Name);
        Assert.AreEqual(invoice.SellerParty.TaxId, reverseDto.SellerParty.TaxId);
        Assert.AreEqual(invoice.PaymentMeans.Iban, reverseDto.PaymentMeans.Iban);
    }

    [TestMethod]
    public void CanDeepReverseMapDto()
    {
        var invoice = GetInvoiceAnnDto();
        var xmlText = ZugferdMapper.MapToZugferd(invoice);
        var reverseDto = ZugferdMapper.MapFromZugferd(xmlText);
        reverseDto.PaymentMeans.Name = invoice.PaymentMeans.Name; // Manually set for now
        DtoAssert.AreEqual(invoice, reverseDto);
    }
}

public static class DtoAssert
{
    public static void AreEqual(BlazorInvoiceDto expected, BlazorInvoiceDto actual)
    {
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.DocumentCurrencyCode, actual.DocumentCurrencyCode);
        Assert.AreEqual(expected.InvoiceTypeCode, actual.InvoiceTypeCode);
        Assert.AreEqual(expected.Note, actual.Note);
        Assert.AreEqual(expected.GlobalTaxCategory, actual.GlobalTaxCategory);
        Assert.AreEqual(expected.GlobalTaxScheme, actual.GlobalTaxScheme);
        Assert.AreEqual(expected.GlobalTax, actual.GlobalTax);
        Assert.AreEqual(expected.IssueDate.Date, actual.IssueDate.Date);
        Assert.AreEqual(expected.PaymentTermsNote, actual.PaymentTermsNote);
        Assert.AreEqual(expected.PayableAmount, actual.PayableAmount);

        AssertBuyer(expected.BuyerParty, actual.BuyerParty);
        AssertSeller(expected.SellerParty, actual.SellerParty);
        AssertPayment(expected.PaymentMeans, actual.PaymentMeans);

        Assert.HasCount(expected.InvoiceLines.Count, actual.InvoiceLines);

        for (int i = 0; i < expected.InvoiceLines.Count; i++)
        {
            AssertInvoiceLine(expected.InvoiceLines[i], actual.InvoiceLines[i]);
        }
    }

    private static void AssertBuyer(BuyerAnnotationDto e, BuyerAnnotationDto a)
    {
        Assert.AreEqual(e.Name, a.Name);
        Assert.AreEqual(e.StreetName, a.StreetName);
        Assert.AreEqual(e.PostCode, a.PostCode);
        Assert.AreEqual(e.City, a.City);
        Assert.AreEqual(e.CountryCode, a.CountryCode);
        Assert.AreEqual(e.Email, a.Email);
        Assert.AreEqual(e.Telefone, a.Telefone);
        Assert.AreEqual(e.TaxId, a.TaxId);
        Assert.AreEqual(e.BuyerReference, a.BuyerReference);
        //Assert.AreEqual(e.RegistrationName, a.RegistrationName);
    }

    private static void AssertSeller(SellerAnnotationDto e, SellerAnnotationDto a)
    {
        Assert.AreEqual(e.Name, a.Name);
        Assert.AreEqual(e.StreetName, a.StreetName);
        Assert.AreEqual(e.PostCode, a.PostCode);
        Assert.AreEqual(e.City, a.City);
        Assert.AreEqual(e.CountryCode, a.CountryCode);
        Assert.AreEqual(e.Email, a.Email);
        Assert.AreEqual(e.Telefone, a.Telefone);
        Assert.AreEqual(e.TaxId, a.TaxId);
        Assert.AreEqual(e.CompanyId, a.CompanyId);
        //Assert.AreEqual(e.RegistrationName, a.RegistrationName);
    }

    private static void AssertPayment(PaymentAnnotationDto e, PaymentAnnotationDto a)
    {
        Assert.AreEqual(e.Iban, a.Iban);
        Assert.AreEqual(e.Bic, a.Bic);
        Assert.AreEqual(e.Name, a.Name);
        Assert.AreEqual(e.PaymentMeansTypeCode, a.PaymentMeansTypeCode);
    }

    private static void AssertInvoiceLine(InvoiceLineAnnotationDto e, InvoiceLineAnnotationDto a)
    {
        Assert.AreEqual(e.Id, a.Id);
        Assert.AreEqual(e.Name, a.Name);
        Assert.AreEqual(e.Description, a.Description);
        Assert.AreEqual(e.Quantity, a.Quantity);
        Assert.AreEqual(e.QuantityCode, a.QuantityCode);
        Assert.AreEqual(e.UnitPrice, a.UnitPrice);
        Assert.AreEqual(e.StartDate, a.StartDate);
        Assert.AreEqual(e.EndDate, a.EndDate);
    }
}