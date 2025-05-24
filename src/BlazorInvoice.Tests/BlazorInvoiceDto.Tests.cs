using BlazorInvoice.Shared;
using pax.XRechnung.NET.AnnotatedDtos;

namespace BlazorInvoice.Tests
{
    [TestClass]
    public sealed class BlazorInvoiceDtoTests
    {
        [TestMethod]
        public void CanMapDto()
        {
            BlazorInvoiceDto invoiceDto = new()
            {
                Note = "Test Note",
                AdditionalDocumentReferences = new List<DocumentReferenceAnnotationDto>
                {
                    new DocumentReferenceAnnotationDto
                    {
                        Id = "123",
                        DocumentDescription = "Test Document",
                        MimeCode = "application/pdf",
                        FileName = "test.pdf",
                        Content = "Base64EncodedContent"
                    }
                },
                SellerParty = new SellerAnnotationDto { Name = "Test Seller", City = "Test City1" },
                BuyerParty = new BuyerAnnotationDto { Name = "Test Buyer", City = "Test City2" }
            };

            var mapper = new BlazorInvoiceMapper();
            var xmlInvoice = mapper.ToXml(invoiceDto);

            Assert.IsNotNull(xmlInvoice, "Mapping to XML failed.");
            Assert.AreEqual(invoiceDto.Note, xmlInvoice.Note, "Note property did not map correctly.");
            Assert.AreEqual(invoiceDto.SellerParty.City, xmlInvoice.SellerParty.Party.PostalAddress.City, "SellerParty City did not map correctly.");
            Assert.AreEqual(invoiceDto.BuyerParty.City, xmlInvoice.BuyerParty.Party.PostalAddress.City, "BuyerParty City did not map correctly.");
            Assert.AreEqual(invoiceDto.SellerParty.Name, xmlInvoice.SellerParty.Party.PartyName.Name, "SellerParty Name did not map corretly.");
            Assert.AreEqual(invoiceDto.BuyerParty.Name, xmlInvoice.BuyerParty.Party.PartyName.Name, "BuyerParty Name did not map corretly.");
        }
    }
}
