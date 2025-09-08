using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace BlazorInvoice.Pwa.Tests;

[TestClass]
public sealed class BasicTests : PageTest
{
    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("http://localhost:5077");
        await Expect(Page).ToHaveTitleAsync(new Regex("BeInX"));
    }

    [TestMethod]
    public async Task NavigateUsingCreateButton()
    {
        await Page.GotoAsync("http://localhost:5077");

        await Page.GetByRole(AriaRole.Button, new() { Name = "New Invoice" }).ClickAsync();

        await Expect(Page).ToHaveTitleAsync(new Regex("New Invoice"));
    }

    [TestMethod]
    public async Task CreateSampleInvoice()
    {
        await Page.GotoAsync("http://localhost:5077");

        await Page.GetByRole(AriaRole.Button, new() { Name = "New Invoice" }).ClickAsync();

        await Expect(Page).ToHaveTitleAsync(new Regex("New Invoice"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Sample Invoice" }).ClickAsync();

        await Expect(Page.Locator("iframe")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ExportSampleInvoice()
    {
        await Page.GotoAsync("http://localhost:5077");

        await Page.GetByRole(AriaRole.Button, new() { Name = "New Invoice" }).ClickAsync();

        await Expect(Page).ToHaveTitleAsync(new Regex("New Invoice"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Sample Invoice" }).ClickAsync();

        await Expect(Page.Locator("iframe")).ToBeVisibleAsync();
        
        await Page.GetByRole(AriaRole.Button, new() { Name = "Export" }).ClickAsync();

        var modal = Page.Locator("#exportModal");
        await Expect(modal).ToBeVisibleAsync();

        await Page.Locator("#typeselect").SelectOptionAsync(new[] { "PdfA3" });

        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await modal.GetByRole(AriaRole.Button, new() { Name = "Export" }).ClickAsync();
        });

        var filePath = Path.Combine(Path.GetTempPath(), "exported-invoice.pdf");
        await download.SaveAsAsync(filePath);

        Assert.IsTrue(File.Exists(filePath), "Exported PDF should exist");
        Assert.IsTrue(new FileInfo(filePath).Length > 0, "Exported PDF should not be empty");
    }
}
