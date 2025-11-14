using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace beinx.PlaywrightTests;

[TestClass]
public sealed class SellersTest : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("https://localhost:6066/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Home"));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Parties" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sellers" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync(new Regex("Sellers"));
    }

    [TestMethod]
    public async Task CanCreateNewSeller()
    {
        // 1️⃣ Navigate to Sellers
        await Page.GotoAsync("https://localhost:6066/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Parties" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sellers" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync(new Regex("Sellers"));

        // 2️⃣ Click "Add New Seller"
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Seller", RegexOptions.IgnoreCase) }).ClickAsync();

        // 3️⃣ Fill out the form using label associations
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

        // 4️⃣ Save
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // 5️⃣ Wait until table reappears
        await Expect(Page.GetByRole(AriaRole.Table)).ToBeVisibleAsync();

        // 6️⃣ Verify entry exists (first table again)
        await Expect(Page.Locator("table").First).ToContainTextAsync("Test Seller");
    }
}
