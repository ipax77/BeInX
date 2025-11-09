using System.Text.RegularExpressions;
using Microsoft.Playwright.MSTest;

namespace beinx.PlaywrightTests;

[TestClass]
public sealed class PaymentTest : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("http://localhost:6066/payments");
        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Payments"));
    }
}
