using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace beinx.PlaywrightTests;

[TestClass]
public sealed class InvoiceTest : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("https://localhost:6066/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Home"));
        await Page.ClickAsync("a[href='invoice']");
        await Expect(Page).ToHaveTitleAsync(new Regex("Invoice"));
    }

    [TestMethod]
    public async Task CanCreateInvoice()
    {
        await Page.GotoAsync("https://localhost:6066/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Home"));
        await Page.ClickAsync("a[href='invoice']");
        await Expect(Page).ToHaveTitleAsync(new Regex("Invoice"));

        // prerequisites
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Select or Create Seller", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Seller", RegexOptions.IgnoreCase) }).ClickAsync();

        await Page.FillAsync("#bt27", "Test Seller");         // Name
        await Page.FillAsync("#bt28", "My Registration");     // Registration Name
        await Page.FillAsync("#bt35", "Main Street 1");       // Street Name
        await Page.FillAsync("#bt38", "12345");               // Postal Zone
        await Page.FillAsync("#bt37", "Test City");           // City Name
        await Page.FillAsync("#bt40", "DE");                  // Country
        await Page.FillAsync("#bt42", "0123456789");          // Telephone
        await Page.FillAsync("#bt43", "test@example.com");    // Email
        await Page.FillAsync("#btweb", "https://example.com");// Website
        await Page.FillAsync("#bt31", "DE999999999");         // Tax ID
        await Page.FillAsync("#bt32", "123/4567/8901");       // Company ID

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        var sellerCount = await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Edit Seller", RegexOptions.IgnoreCase) }).CountAsync();
        Assert.AreEqual(1, sellerCount);

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Select or Create Buyer", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Buyer", RegexOptions.IgnoreCase) }).ClickAsync();

        await Page.FillAsync("#bt27", "Test Buyer");         // Name
        await Page.FillAsync("#bt28", "My Registration");     // Registration Name
        await Page.FillAsync("#bt35", "Main Street 1");       // Street Name
        await Page.FillAsync("#bt38", "12345");               // Postal Zone
        await Page.FillAsync("#bt37", "Test City");           // City Name
        await Page.FillAsync("#bt40", "DE");                  // Country
        await Page.FillAsync("#bt42", "0123456789");          // Telephone
        await Page.FillAsync("#bt43", "test@example.com");    // Email
        await Page.FillAsync("#bt10", "test@example.com");    // Buyer Reference

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        var buyerCount = await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Edit Buyer", RegexOptions.IgnoreCase) }).CountAsync();
        Assert.AreEqual(1, buyerCount);

        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Select or Create Payment Means", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Payment Means", RegexOptions.IgnoreCase) }).ClickAsync();

        await Page.FillAsync("#name", "Test Bank");                // Name
        await Page.FillAsync("#iban", "DE12345678901234567890");   // Registration Name
        await Page.FillAsync("#bic", "TESTDEFFXXX");               // Street Name
        await Page.FillAsync("#bt81", "30");                       // Street Name

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        var paymentCount = await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Edit Payment Means", RegexOptions.IgnoreCase) }).CountAsync();
        Assert.AreEqual(1, paymentCount);
    }
}
