using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace beinx.PlaywrightTests;

[TestClass]
public sealed class PaymentTest : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("https://localhost:6066/");
        await Expect(Page).ToHaveTitleAsync(new Regex("Home"));
        await Page.ClickAsync("a[href='payments']"); // or appropriate selector
        await Expect(Page).ToHaveTitleAsync(new Regex("Payments"));
    }

    [TestMethod]
    public async Task CanCreateNewPayment()
    {
        // 1️⃣ Go to the payments page
        await Page.GotoAsync("https://localhost:6066/");
        await Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Payment", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync(new Regex("Payments"));

        // 2️⃣ Click "Add New Payment Means"
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Payment", RegexOptions.IgnoreCase) }).ClickAsync();

        // 3️⃣ Fill the form
        await Page.FillAsync("#name", "Test Bank");
        await Page.FillAsync("#iban", "DE12345678901234567890");
        await Page.FillAsync("#bic", "TESTDEFFXXX");
        await Page.FillAsync("#bt81", "31");

        // 4️⃣ Submit the form
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save", RegexOptions.IgnoreCase) }).ClickAsync();

        // 5️⃣ Wait for the table to reappear (editing mode ends)
        await Expect(Page.GetByRole(AriaRole.Table)).ToBeVisibleAsync();

        // 6️⃣ Verify the new entry is in the table
        await Expect(Page.Locator("table").First).ToContainTextAsync("Test Bank");
        await Expect(Page.Locator("table").First).ToContainTextAsync("DE12345678901234567890");
    }

    [TestMethod]
    public async Task CanPerformFullPaymentCrudFlow()
    {
        string _bankName = $"TestBank-{Guid.NewGuid():N}"[..8];
        const string _iban = "DE12345678901234567890";
        const string _bic = "TESTDEFFXXX";
        const string _paymentCode = "31";

        // 1️⃣ Navigate to the Payments page
        await Page.GotoAsync("https://localhost:6066/");
        await Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Payment", RegexOptions.IgnoreCase) }).ClickAsync();

        // --- CREATE ---
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Add New Payment", RegexOptions.IgnoreCase) }).ClickAsync();

        await Page.GetByLabel("Bank Name").FillAsync(_bankName);
        await Page.GetByLabel("IBAN").FillAsync(_iban);
        await Page.GetByLabel("BIC").FillAsync(_bic);
        await Page.GetByLabel("Payment Code").FillAsync(_paymentCode);
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save", RegexOptions.IgnoreCase) }).ClickAsync();

        // Wait for table to reappear and contain our new record
        await Expect(Page.GetByRole(AriaRole.Table)).ToBeVisibleAsync();
        await Expect(Page.Locator("table").First).ToContainTextAsync(_bankName);

        // --- UPDATE ---
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Edit", RegexOptions.IgnoreCase) }).ClickAsync();
        await Page.GetByLabel("Bank Name").FillAsync($"{_bankName}-Edited");
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Save", RegexOptions.IgnoreCase) }).ClickAsync();

        await Expect(Page.Locator("table").First).ToContainTextAsync($"{_bankName}-Edited");

        // --- DELETE ---
        // Click Delete button for that entry
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Delete", RegexOptions.IgnoreCase) }).ClickAsync();

        // Wait for table update (Blazor re-render)
        await Expect(Page.Locator("table").First).Not.ToContainTextAsync($"{_bankName}-Edited");
    }
}
